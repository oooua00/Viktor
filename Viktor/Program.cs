using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = System.Drawing.Color;
using SharpDX;
using LeagueSharp;
using LeagueSharp.Common;
using LeagueSharp.Common.Data;
using Predictions;

namespace Viktor
{
    static class Program
    {
        private static Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        private static Orbwalking.Orbwalker _orbwalker;
        private static Spell _q, _e, _r;
        private static Spells E, R;
        private static Menu _menu;
        private static GameObject ViktorR = null;

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.ChampionName != "Viktor")
                return;

            _q = new Spell(SpellSlot.Q);
            _e = new Spell(SpellSlot.E);
            _r = new Spell(SpellSlot.R);
            E = new Spells(SpellSlot.E, SkillshotType.SkillshotLine, 520, (float)0.25, 40, false, 780, 500);
            R = new Spells(SpellSlot.R, SkillshotType.SkillshotCircle, 700, 0.25f, 325 / 2, false);

            _menu = new Menu(Player.ChampionName, Player.ChampionName, true);
            Menu orbwalkerMenu = new Menu("Orbwalker", "Orbwalker");
            _orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);
            _menu.AddSubMenu(orbwalkerMenu);
            Menu ts = _menu.AddSubMenu(new Menu("Target Selector", "Target Selector")); ;
            TargetSelector.AddToMenu(ts);

            Menu spellMenu = _menu.AddSubMenu(new Menu("Spells", "Spells"));
            Menu Harass = spellMenu.AddSubMenu(new Menu("Harass", "Harass"));
            Menu Combo = spellMenu.AddSubMenu(new Menu("Combo", "Combo"));
            Menu Focus = spellMenu.AddSubMenu(new Menu("Focus Selected", "Focus Selected"));
            Menu KS = spellMenu.AddSubMenu(new Menu("KillSteal", "KillSteal"));
            Menu drawingg = spellMenu.AddSubMenu(new Menu("drawing", "drawing"));
            Harass.AddItem(new MenuItem("Use Q Harass", "Use Q Harass").SetValue(true));
            Harass.AddItem(new MenuItem("Use E Harass", "Use E Harass").SetValue(true));
            Combo.AddItem(new MenuItem("Use Q Combo", "Use Q Combo").SetValue(true));
            Combo.AddItem(new MenuItem("Use E Combo", "Use E Combo").SetValue(true));
            Combo.AddItem(new MenuItem("Use R Burst Selected", "Use R Burst Selected").SetValue(true));
            Focus.AddItem(new MenuItem("force focus selected", "force focus selected").SetValue(false));
            Focus.AddItem(new MenuItem("if selected in :", "if selected in :").SetValue(new Slider(1000, 1000, 1500)));
            KS.AddItem(new MenuItem("Use Q KillSteal", "Use Q KillSteal").SetValue(true));
            KS.AddItem(new MenuItem("Use E KillSteal", "Use E KillSteal").SetValue(true));
            KS.AddItem(new MenuItem("Use R KillSteal", "Use R KillSteal").SetValue(true));
            spellMenu.AddItem(new MenuItem("Use R Follow", "Use R Follow").SetValue(true));
            drawingg.AddItem(new MenuItem("apollo.viktor.draw.cd", "Draw on CD").SetValue(new Circle(false, Color.DarkRed)));
                MenuItem drawComboDamageMenu = new MenuItem("apollo.viktor.draw.ind.bool", "Draw Combo Damage", true).SetValue(true);
                MenuItem drawFill = new MenuItem("apollo.viktor.draw.ind.fill", "Draw Combo Damage Fill", true).SetValue(new Circle(true, Color.FromArgb(90, 255, 169, 4)));
                drawingg.AddItem(drawComboDamageMenu);
                drawingg.AddItem(drawFill);
                DamageIndicator.DamageToUnit = drawing.ComboDmg;
                DamageIndicator.Enabled = drawComboDamageMenu.GetValue<bool>();
                DamageIndicator.Fill = drawFill.GetValue<Circle>().Active;
                DamageIndicator.FillColor = drawFill.GetValue<Circle>().Color;
                drawComboDamageMenu.ValueChanged +=
                    delegate(object sender, OnValueChangeEventArgs eventArgs)
                    {
                        DamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
                    };
                drawFill.ValueChanged +=
                    delegate(object sender, OnValueChangeEventArgs eventArgs)
                    {
                        DamageIndicator.Fill = eventArgs.GetNewValue<Circle>().Active;
                        DamageIndicator.FillColor = eventArgs.GetNewValue<Circle>().Color;
                    };
        
            _menu.AddToMainMenu();

            Game.OnUpdate += Game_OnGameUpdate;
            GameObject.OnCreate += Create;
            GameObject.OnDelete += Delete;
            Game.PrintChat("Welcome to ViktorWorld");
        }
        private static void Create(GameObject sender, EventArgs args)
        {
            if (sender.Name.Contains("Viktor_Base_R_Droid.troy"))
            {
                ViktorR = sender;
            }
        }
        private static void Delete(GameObject sender, EventArgs args)
        {
            if (sender.Name.Contains("Viktor_Base_R_Droid.troy"))
            {
                ViktorR = null;
            }
        }
        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead)
                return;
            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo || (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed && Selected()))
            {
                if (_q.IsReady() && Player.Mana >= _q.Instance.ManaCost)
                {
                    _orbwalker.SetAttack(false);
                }
                if (!_q.IsReady() || _q.IsReady() && Player.Mana < _q.Instance.ManaCost)
                {
                    _orbwalker.SetAttack(true);
                }
            }
            else
            {
                _orbwalker.SetAttack(true);
            }

            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                if (_menu.Item("Use Q Combo").GetValue<bool>())
                {
                    UseQ();
                }
                if (_menu.Item("Use E Combo").GetValue<bool>())
                {
                    UseE();
                }
                if (_menu.Item("Use R Combo Selected").GetValue<bool>())
                {
                    UseR();
                }
            }
            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                if (_menu.Item("Use Q Harass").GetValue<bool>())
                    UseQ();
                if (_menu.Item("Use E Harass").GetValue<bool>())
                    UseE();
            }
            ViktorRMove();
            killsteal();
        }
        private static void ViktorRMove()
        {
            if (_menu.Item("Use R Follow").GetValue<bool>() && ViktorR != null && _r.IsReady())
            {
                var target = ViktorR.Position.GetEnemiesInRange(2000).OrderByDescending(t => 1 - t.Distance(ViktorR.Position)).FirstOrDefault();
                if (target.Distance(ViktorR.Position) >= 50)
                {
                    Vector3 x = Prediction.GetPrediction(target, 150).UnitPosition;
                    _r.Cast(x);
                }
            }
        }
        private static bool Selected()
        {
            if (!_menu.Item("force focus selected").GetValue<bool>())
            {
                return false;
            }
            else
            {
                var target = TargetSelector.GetSelectedTarget();
                float a = _menu.Item("if selected in :").GetValue<Slider>().Value;
                if (target == null || target.IsDead || target.IsZombie)
                {
                    return false;
                }
                return !(Player.Distance(target.Position) > a);
            }
        }

        private static Obj_AI_Base Gettarget(float range)
        {
            return Selected() ? TargetSelector.GetSelectedTarget() : TargetSelector.GetTarget(range, TargetSelector.DamageType.Magical);
        }

        private static void UseQ()
        {
            var target = Gettarget(600);
            if (target != null && target.IsValidTarget() && !target.IsZombie && _q.IsReady())
                _q.Cast(target);
        }

        private static void UseR()
        {
            var target = TargetSelector.GetSelectedTarget();
            if (target != null && Player.Distance(target.Position) <= 950 && target.IsValidTarget() && !target.IsZombie && _r.IsReady() && _r.Instance.Name == "ViktorChaosStorm")
            {
                CastR(target);
            }
        }

        private static void CastR(Obj_AI_Base target)
        {

            R.Cast(target);
        }

        private static void UseE()
        {
            var target = Gettarget(1025);
            if (target != null && target.IsValidTarget() && !target.IsZombie && _e.IsReady())
            {

                Vector3 x = Player.Distance(target.Position) >= 525 ? Player.Position.Extend(target.Position, 525) : target.Position;
                E.Cast(true, x, target);
            }
        }
        public static void killsteal()
        {
            if (_q.IsReady() && _menu.Item("Use Q KillSteal").GetValue<bool>() && !Player.IsWindingUp)
            {
                foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy && hero.IsValidTarget(600)))
                {
                    var dmg = Dame(hero, SpellSlot.Q);
                    if (hero != null && hero.IsValidTarget() && !hero.IsZombie && dmg > hero.Health) { _q.Cast(hero); }
                }
            }
            if (_e.IsReady() && _menu.Item("Use E KillSteal").GetValue<bool>() && !Player.IsWindingUp)
            {
                foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy && hero.IsValidTarget(1025)))
                {
                    var dmg = Dame(hero, SpellSlot.E);
                    if (hero != null && hero.IsValidTarget() && !hero.IsZombie && dmg > hero.Health)
                    {
                        Vector3 x = Player.Distance(hero.Position) >= 525 ? Player.Position.Extend(hero.Position, 525) : hero.Position;
                        E.Cast(true, x, hero);
                    }
                }
            }

            if (_r.IsReady() && _menu.Item("Use R KillSteal").GetValue<bool>() && !Player.IsWindingUp)
            {
                foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy && hero.IsValidTarget(860)))
                {
                    var dmgR = Dame(hero, SpellSlot.R);
                    var dmgE = Dame(hero, SpellSlot.E);
                    var dmgQ = Dame(hero, SpellSlot.Q);
                    if (hero != null && hero.IsValidTarget() && !hero.IsZombie)
                    {
                        if (dmgE > hero.Health && dmgR > hero.Health)
                        {
                            if (E.Instance.Cooldown - E.Instance.CooldownExpires >= 450 && !E.IsReady())
                                R.Cast(hero);
                        }
                        else if (dmgQ > hero.Health && dmgR > hero.Health && Player.Distance(hero.Position) <= 600)
                        {
                            if (_q.Instance.Cooldown - _q.Instance.CooldownExpires >= 450 && !_q.IsReady() && !E.IsReady())
                                R.Cast(hero);
                        }
                        else if (dmgR > hero.Health) { R.Cast(hero); }
                    }
                }
            }

        }
        public static double Dame(Obj_AI_Base target, SpellSlot x)
        {
            if (target != null) { return Player.GetSpellDamage(target, x); } else return 0;
        }

    }
}
