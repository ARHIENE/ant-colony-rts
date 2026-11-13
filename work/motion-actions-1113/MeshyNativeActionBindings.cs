using System.Collections.Generic;
namespace AntColony.EditorTools
{
    // Only existing game actions with a suitable collected source. Context animations
    // such as ladders stay in the library until that interaction exists in the game.
    public static class MeshyNativeActionBindings
    {
        public static Dictionary<string,string> All()
        {
            var map=new Dictionary<string,string>(MeshyMotionImport.Actions);
            foreach(var p in new[]{
                ("Tool_Dig","Heavy_Hammer_Swing"),("Tool_Till","Heavy_Hammer_Swing"),("Attack_Unarmed","Counterstrike"),
                ("WalkLimp","Limping_Walk_1"),("WalkLimpSevere","Limping_Walk_3"),("LeisureWalk","Casual_Walk"),
                ("IdleTalk","Talk_Passionately"),("IdleRest","Sit_Cross_Legged_on_Floor"),("IdleLook","Short_Breathe_and_Look_Around"),
                ("Drink","Stand_and_Drink"),("WakeUp","Arise"),
                ("WalkTipsy","Stumble_Walk"),("Stagger","Stumble_Walk"),
                ("PassOut","falling_down"),("CollapseDropWeapon","falling_down"),
                ("HangoverIdle","Headache_Relief"),("Mourn","Head_Hold_in_Pain"),
                ("Hurt","Hit_Reaction"),("Pickup","Male_Bend_Over_Pick_Up"),
                ("EquipFromGround","Male_Bend_Over_Pick_Up"),("BenchPickup","Collect_Object"),
                ("PutDown","Collect_Object"),("BenchPlace","Collect_Object"),
                ("UnequipToGround","Collect_Object"),("Captive","Sit_Cross_Legged_on_Floor"),
                ("StandIdle","Idle_11"),("Idle_A","Combat_Stance"),
                ("Idle","Idle_3"),("StandEnter","Sit_to_Stand_Transition_M"),
                ("StandExit","Stand_to_Sit_Transition_M")
            })map[p.Item1]=p.Item2;
            map["WalkLimpAlternate"] = "Limping_Walk_2";
            var runs = new[] { "Running", "Run_02", "Run_03" };
            var talks = new[] { "Talk_Passionately", "Talk_with_Hands_Open", "Talk_with_Left_Hand_Raised", "Talk_with_Right_Hand_Open" };
            var cheers = new[] { "Victory_Fist_Pump", "Cheer_with_Both_Hands", "Victory_Cheer", "victory" };
            for (int i = 0; i < runs.Length; i++) map["RunVariant" + i] = runs[i];
            for (int i = 0; i < talks.Length; i++) map["TalkVariant" + i] = talks[i];
            for (int i = 0; i < cheers.Length; i++) map["CelebrateVariant" + i] = cheers[i];
            for (int i = 0; i < 11; i++) map["StandVariant" + i] = "Idle_" + (i + 3);
            map["ListenAgree"] = "Agree_Gesture";
            return map;
        }
    }
}
