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
            return map;
        }
    }
}
