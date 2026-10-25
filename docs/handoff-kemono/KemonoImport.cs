using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace AntColony.EditorTools
{
    // 2026-10-24 kemono 캐릭터 교체: art/production-kit-20261019/exports → Unity 반입 설정과 Animator 상태 구성.
    // 캐릭터·컨트롤러는 런타임 Resources.Load로 쓰고(프리팹은 커밋 제외 규칙이라 만들지 않음), 동작 FBX는 Art/Kemono/Anim에 둔다.
    public static class KemonoImport
    {
        public const string Character = "Assets/Resources/Kemono/Kemono_Character.fbx";
        public const string Controller = "Assets/Resources/Kemono/KemonoAnimator.controller";
        const string AnimDir = "Assets/_Project/Art/Kemono/Anim";
        static readonly string[] Loops = { "Idle", "StandIdle", "Walk", "Run", "Carry", "Chop", "Mine", "Hammer", "Sit", "CarryPerson", "GroundWork", "BenchWork",
            "Treat", "Operate", "Stir", "Grind", "Saw", "Stamp", "Wash", "WaterPlant", "RockCradle", "Inspect", "PlayMusic", "Exercise", "Wrestle", "Fly", "Rest",
            "PlayBoard", "Eat", "Bathe", "Captive", "Swing", "Spin", "Sleep", "Downed", "Mourn" };

        [MenuItem("AntColony/Kemono/Configure Import + Animator")]
        public static string Run()
        {
            var model = (ModelImporter)AssetImporter.GetAtPath(Character);
            model.animationType = ModelImporterAnimationType.Generic; model.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            model.importAnimation = false; model.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            model.SaveAndReimport();
            var avatar = AssetDatabase.LoadAllAssetsAtPath(Character).OfType<Avatar>().First();

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Controller) ?? AnimatorController.CreateAnimatorControllerAtPath(Controller);
            var sm = controller.layers[0].stateMachine;
            foreach (var s in sm.states.ToArray()) sm.RemoveState(s.state);
            var count = 0;
            foreach (var path in Directory.GetFiles(AnimDir, "*.fbx").Select(p => p.Replace('\\', '/')))
            {
                var name = Path.GetFileNameWithoutExtension(path).Split('@')[1].Replace("PK_", "");
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.animationType = ModelImporterAnimationType.Generic; importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                importer.sourceAvatar = avatar; importer.importAnimation = true; importer.materialImportMode = ModelImporterMaterialImportMode.None;
                var clips = importer.defaultClipAnimations;
                var pk = Path.GetFileName(path).Contains("@PK_");
                foreach (var c in clips) { c.name = name; c.loopTime = Loops.Contains(name) || pk && LoopPk(path); }
                importer.clipAnimations = clips; importer.SaveAndReimport();
                var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview__"));
                var state = sm.AddState(name); state.motion = clip; count++;
                if (name == "Idle") sm.defaultState = state;
            }
            EditorUtility.SetDirty(controller); AssetDatabase.SaveAssets();
            return $"kemono states {count}";
        }

        // PK 클립 반복 여부는 export-list.json(loop)과 같게: 원본 json을 읽지 않고 내보낸 쪽 이름 규칙으로 판정한다.
        static bool LoopPk(string path)
        {
            var n = Path.GetFileNameWithoutExtension(path);
            string[] once = { "DrawSword", "SheatheSword", "Muster", "ToolOut", "ToolAway", "CollapseDropWeapon", "EquipFromGround", "UnequipToGround",
                "BenchPlace", "BenchPickup", "OvenLoad", "PourSack", "TapFill", "Drink", "PassOut", "CanisterConnect", "CanisterDisconnect", "LatrineSquat", "WakeUp" };
            return !once.Any(o => n.EndsWith("PK_" + o));
        }
    }
}
