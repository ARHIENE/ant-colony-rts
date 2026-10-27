using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace AntColony.EditorTools
{
    public static class KemonoImport
    {
        public const string Character = "Assets/Resources/Kemono/Kemono_Character.fbx";
        public const string Controller = "Assets/Resources/Kemono/KemonoAnimator.controller";
        const string AnimDir = "Assets/_Project/Art/Kemono/Anim";
        const string MaterialDir = "Assets/Resources/Kemono/Materials";
        static readonly string[] Loops = { "Idle", "StandIdle", "Walk", "Run", "Carry", "Chop", "Mine", "Hammer", "Sit", "CarryPerson", "GroundWork", "BenchWork",
            "Treat", "Operate", "Stir", "Grind", "Saw", "Stamp", "Wash", "WaterPlant", "RockCradle", "Inspect", "PlayMusic", "Exercise", "Wrestle", "Fly", "Rest",
            "PlayBoard", "Eat", "Bathe", "Captive", "Swing", "Spin", "Sleep", "Downed", "Mourn" };
        // exports/animations/export-list.json의 loop 값.
        static readonly string[] ProductionLoops = { "BeatFire", "BowShoot", "ControlPanel", "Crave", "DriveSeated", "HangoverIdle", "Knead", "Ladle", "PressLever",
            "SelfTreat", "SewRepair", "ShieldBlock", "SkyGaze", "SmithRepair", "SpearThrust", "Stagger", "SwordAttack", "Tool_Chop", "Tool_Dig", "Tool_Hammer",
            "Tool_Harvest", "Tool_Mine", "Tool_Till", "ValveTurn", "WalkTipsy" };
        static readonly string[] Facilities = { "OilDerrick", "Refinery", "LiquidTank", "PipeL_Straight", "Deposit_Oil_Normal", "FillingStation", "ManualWell",
            "CrashWreck", "School", "CitizenClinic", "CivicPublicService", "PlanterBed", "WaterSource", "WaterPump", "SteamGenerator" };

        [MenuItem("AntColony/Kemono/Configure Import + Animator")]
        public static string Run()
        {
            try { var result = Configure(); File.WriteAllText("Temp/kemono-import-result.txt", result); return result; }
            catch (Exception e) { File.WriteAllText("Temp/kemono-import-result.txt", e.ToString()); throw; }
        }

        static string Configure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before importing kemono.");
            AssetDatabase.Refresh();
            var model = (ModelImporter)AssetImporter.GetAtPath(Character);
            ConfigureModel(model);
            model.animationType = ModelImporterAnimationType.Generic;
            model.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            model.importAnimation = false;
            model.SaveAndReimport();
            var avatar = AssetDatabase.LoadAllAssetsAtPath(Character).OfType<Avatar>().Single();
            if (!avatar.isValid) throw new InvalidOperationException("Kemono Generic avatar is invalid.");
            ConvertMaterials(Character);

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Controller) ?? AnimatorController.CreateAnimatorControllerAtPath(Controller);
            var sm = controller.layers[0].stateMachine;
            var states = sm.states.ToDictionary(s => s.state.name, s => s.state);
            void State(string name, AnimationClip clip)
            {
                if (!states.TryGetValue(name, out var state)) states[name] = state = sm.AddState(name);
                state.motion = clip;
                if (name == "Idle_A") sm.defaultState = state;
            }
            var count = 0;
            foreach (var path in Directory.GetFiles(AnimDir, "*.fbx").OrderBy(p => p).Select(p => p.Replace('\\', '/')))
            {
                var raw = Path.GetFileNameWithoutExtension(path).Split('@')[1];
                var production = raw.StartsWith("PK_");
                var name = production ? raw.Substring(3) : raw;
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                ConfigureModel(importer);
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                importer.sourceAvatar = avatar;
                importer.importAnimation = true;
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                // Generic 설정을 적용한 뒤 Take 범위를 읽는다.
                importer.SaveAndReimport();
                var clips = importer.defaultClipAnimations;
                foreach (var c in clips)
                {
                    c.name = name;
                    c.loopTime = (production ? ProductionLoops : Loops).Contains(name);
                    c.lockRootRotation = c.lockRootHeightY = c.lockRootPositionXZ = true;
                }
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
                var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview__"));
                State(name, clip);
                if (!production && name == "Attack") State("Attack_Unarmed", clip);
                if (name == "Idle") State("Idle_A", clip);
                if (name == "Hurt") State("Hit", clip);
                // 기존 검사와 호출자가 실제 Base Layer.Attack 상태도 확인한다.
                if (name == "SwordAttack") State("Attack", clip);
                count++;
            }
            foreach (var path in Directory.GetFiles("Assets/Resources/Kemono/Handheld", "*.fbx")) ImportProp(path.Replace('\\', '/'));
            foreach (var path in Directory.GetFiles("Assets/Resources/Kemono/Props", "*.fbx")) ImportProp(path.Replace('\\', '/'));
            foreach (var name in Facilities) ImportProp("Assets/Resources/Facilities/CC_" + name + ".fbx");
            foreach (var name in new[] { "Idle_A", "Walk", "Run", "Eat", "Attack", "Hit", "Fly", "Sit", "Death", "Captive", "Tool_Hammer", "Tool_Chop", "Tool_Mine", "Tool_Dig", "Tool_Harvest" })
                if (!states.ContainsKey(name)) throw new InvalidOperationException("Missing kemono state: " + name);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return $"kemono clips {count}, states {sm.states.Length}, avatar valid {avatar.isValid}";
        }

        static void ConfigureModel(ModelImporter importer)
        {
            importer.importCameras = importer.importLights = importer.addCollider = false;
            importer.optimizeGameObjects = false; // 도구 소켓과 선택형 부품을 런타임에서 찾는다.
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
        }

        static void ImportProp(string path, bool refresh = true)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            ConfigureModel(importer);
            importer.importAnimation = false;
            importer.SaveAndReimport();
            ConvertMaterials(path, refresh);
        }

        [Serializable] class SourceMaterial { public string name; public float[] color; public bool linked; }
        [Serializable] class SourceMaterials { public SourceMaterial[] materials; }

        static void ConvertMaterials(string path, bool refresh = true)
        {
            Directory.CreateDirectory(MaterialDir);
            if (refresh) AssetDatabase.Refresh();
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit shader is missing.");
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            // 지워진 머티리얼을 가리키는 리맵이 남으면 원본 머티리얼이 안 나와 다시 만들 수 없다(2026-10-24).
            var stale = importer.GetExternalObjectMap().Where(kv => kv.Value == null).Select(kv => kv.Key).ToList();
            if (stale.Count > 0) { foreach (var k in stale) importer.RemoveRemap(k); importer.SaveAndReimport(); }
            var originals = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>()
                .Select(m => (name: m.name, material: m))
                .Concat(importer.GetExternalObjectMap().Where(kv => kv.Key.type == typeof(Material) && kv.Value is Material)
                    .Select(kv => (name: kv.Key.name, material: (Material)kv.Value)))
                .GroupBy(m => m.name).Select(g => g.First()).ToArray();
            var sourcePath = "Assets/Resources/Kemono/Textures/SourceMaterials.json";
            var source = path == Character && File.Exists(sourcePath) ? JsonUtility.FromJson<SourceMaterials>(File.ReadAllText(sourcePath)) : null;
            foreach (var entry in originals)
            {
                var original = entry.material;
                var name = Path.GetFileNameWithoutExtension(path) + "_" + entry.name;
                foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
                var materialPath = MaterialDir + "/" + name + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    material = new Material(shader);
                    var color = original.HasProperty("_BaseColor") ? original.GetColor("_BaseColor") : original.HasProperty("_Color") ? original.color : Color.white;
                    var texture = original.mainTexture;
                    if (path.EndsWith("CC_ManualWell.fbx")) texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ThirdParty/LowPoly Fantasy Village/Textures/Texture_01.png");
                    // Blender 노드(색 분리·혼합)를 거친 텍스처는 FBX에 안 실려 원본을 직접 연결한다(2026-10-24).
                    var known = entry.name switch { "Face.001" => "Face", "Eye.001" => "Iris", "Hair.001" or "Hair lightReflect.001" => "Hair_1", "Settler_RecoloredSourceCloth.001" => "Linen_Baked", _ => null };
                    if (path == Character && known != null) { texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Kemono/Textures/" + known + ".png"); color = Color.white; }
                    material.SetColor("_BaseColor", color);
                    material.SetTexture("_BaseMap", texture);
                    material.SetFloat("_Smoothness", 0);
                    AssetDatabase.CreateAsset(material, materialPath);
                }
                if (path == Character)
                {
                    var data = source?.materials.FirstOrDefault(m => m.name == entry.name);
                    if (data != null && !data.linked)
                        material.SetColor("_BaseColor", new Color(data.color[0], data.color[1], data.color[2], data.color[3]).gamma);
                    if (entry.name == "Settler_RecoloredSourceCloth.001")
                    {
                        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Kemono/Textures/Linen_Baked.png"));
                        material.SetColor("_BaseColor", Color.white);
                    }
                    EditorUtility.SetDirty(material);
                }
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), entry.name), material);
            }
            importer.SaveAndReimport();
        }
    }
}


