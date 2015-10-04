using System;
using System.Linq;
using SharpDX;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;

namespace NC_ALISTAR
{
    class Program
    {

        public static int version = 01;

        public static Spell.Active Q;
        public static Spell.Targeted W;
        public static Spell.Active E;
        public static Spell.Active R;

        public static Menu menu,
                drawMenu,
                miscMenu,
                laneClear,
                autoE;

        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += OnLoadingComplete;
            Game.OnTick += OnTick;
            Drawing.OnDraw += Game_OnDraw;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast;
        }



        private static void Game_OnDraw(EventArgs args)
        {
            if (drawMenu["drawW"].Cast<CheckBox>().CurrentValue && !_Player.IsDead)
            {

                if (W.IsReady() && Q.IsReady() && !_Player.IsDead)
                {
                    Circle.Draw(Color.Lime, W.Range, _Player.Position);
                    Drawing.DrawText(_Player.Position.WorldToScreen().X, _Player.Position.WorldToScreen().Y, System.Drawing.Color.Lime, "Combo ready");
                }
                else
                {
                    Circle.Draw(Color.Red, W.Range, _Player.Position);
                    Drawing.DrawText(_Player.Position.WorldToScreen().X, _Player.Position.WorldToScreen().Y, System.Drawing.Color.Red, "Combo dont ready");
                }

            }

            if (drawMenu["drawQ"].Cast<CheckBox>().CurrentValue)
                Circle.Draw(Color.Blue, Q.Range, _Player.Position);


        }

        private static void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && !(miscMenu["stopULT"].Cast<CheckBox>().CurrentValue))
                return;

            if (args.SData.Name.Equals("LuxMaliceCannon") || args.SData.Name.Equals("LuxMaliceCannonMis") && miscMenu["stopULTLux"].Cast<CheckBox>().CurrentValue)
            {
                if (Q.IsReady() && Q.IsInRange(sender))
                {
                    Q.Cast();
                }

            }
            else if (args.SData.Name.Equals("EzrealTrueshotBarrage") && miscMenu["stopULTEzreal"].Cast<CheckBox>().CurrentValue)
            {
                if (Q.IsReady() && Q.IsInRange(sender))
                {
                    Q.Cast();
                }
                else if (W.IsReady() && W.IsInRange(sender))
                {
                    W.Cast(sender);
                }
            }
        }

        public static AIHeroClient _Player
        {
            get { return ObjectManager.Player; }
        }

        private static void OnLoadingComplete(EventArgs args)
        {
            SkillsManager();
            _Menu();
        }

        private static void OnTick(EventArgs args)
        {
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Combo)
                _Combo();

            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.LaneClear || Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.LastHit)
                _FarmQ();

            if (autoE["autoE"].Cast<CheckBox>().CurrentValue && (_Player.ManaPercent >= autoE["allyHPMana"].Cast<Slider>().CurrentValue))
                _HealAlly();

        }

        private static void _Menu()
        {
            Chat.Print("<font color='#2fcbff'>[NECEK-CARRY]</font><font color='#b9ebe3'> Alistar loaded, have fun! Version: </font>" + version);

            menu = MainMenu.AddMenu("[NC] Alitar", "alistar");
            menu.AddGroupLabel("[NC] Alistar");
            menu.AddLabel("= x22necek, i love poland. =");

            drawMenu = menu.AddSubMenu("Draw Settings", "drawsettings");
            drawMenu.AddGroupLabel("Draw Settings");
            drawMenu.Add("drawW", new CheckBox("Draw combo status / W Range", true));
            drawMenu.Add("drawQ", new CheckBox("Q Range", false));

            laneClear = menu.AddSubMenu("Lane Clear", "laneclear");
            laneClear.AddGroupLabel("Lane Clear");
            laneClear.Add("autoQ", new CheckBox("Auto Q if minion is killable", true));
            laneClear.Add("amountMinion", new Slider("Minimum amount minion to Q", 3, 2, 10));

            autoE = menu.AddSubMenu("Auto E", "autoe");
            autoE.AddGroupLabel("Auto E");
            autoE.Add("autoE", new CheckBox("Use spell E to heal ally", true));
            autoE.Add("allyHP", new Slider("Heal ally if % hp is less than", 10, 1, 100));

            autoE.AddSeparator();
            autoE.AddGroupLabel("Minimum Mana");
            autoE.Add("allyHPMana", new Slider("Minimum % Mana to spell E", 40, 1, 100));

            miscMenu = menu.AddSubMenu("Misc");
            miscMenu.AddGroupLabel("Break ult");
            miscMenu.Add("stopULT", new CheckBox("Break dangerous ult", true));
            miscMenu.AddLabel("Check dangerous ult");
            miscMenu.Add("stopULTKatarina", new CheckBox("Katarina ULT -- momentarily disabled", true));
            miscMenu.Add("stopULTEzreal", new CheckBox("Ezreal ULT", true));
            miscMenu.Add("stopULTLux", new CheckBox("Lux ULT", true));
            miscMenu.AddSeparator();
            miscMenu.Add("debug", new CheckBox("Debug", false));
        }

        private static void _Combo()
        {
            var t = TargetSelector.GetTarget(W.Range, DamageType.Magical);

            if (!(t != null))
                return;

            if (Q.IsReady() && Q.IsInRange(t) && !_Player.IsZombie)
            {
                Q.Cast();
            }
            else if (W.IsReady() && W.IsInRange(t) && !_Player.IsZombie && Q.IsReady())
            {
                W.Cast(t);
                if (Q.IsReady())
                {
                    System.Threading.Thread.Sleep(100);
                    Q.Cast();
                }
            }
        }

        private static void SkillsManager()
        {
            Q = new Spell.Active(SpellSlot.Q, 320);
            W = new Spell.Targeted(SpellSlot.W, 580);
            E = new Spell.Active(SpellSlot.E, 525);
            R = new Spell.Active(SpellSlot.R);

        }

        private static void _FarmQ()
        {
            if (!(laneClear["autoQ"].Cast<CheckBox>().CurrentValue))
                return;
            var minions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Player.Instance.Position, Q.Range).Where(minion => minion != null && !minion.IsDead && minion.Health < _CalcQ(minion)).ToList();

            if (minions.Count >= laneClear["amountMinion"].Cast<Slider>().CurrentValue)
            {
                if (Q.IsReady())
                    Q.Cast();
            }
        }

        private static void _HealAlly()
        {
            var allys = EntityManager.Heroes.Allies.Where(ally => ally != null && ally.IsAlly && !(ally.IsMe) && E.IsInRange(ally) && ally.HealthPercent <= autoE["allyHP"].Cast<Slider>().CurrentValue).ToList();

            if (allys.Count >= 1)
            {
                if (E.IsReady())
                    E.Cast();
            }

        }


        private static float _CalcQ(Obj_AI_Base target)
        {
            float dmg = _Player.GetSpellDamage(target, SpellSlot.Q);
            if (miscMenu["debug"].Cast<CheckBox>().CurrentValue)
                Chat.Print(dmg);
            return dmg;
        }

    }
}
