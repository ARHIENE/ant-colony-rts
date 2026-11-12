using AntColony.World;
using UnityEngine;

namespace AntColony.Units
{
    // Visuals follow simulation; animation never changes damage, movement or resource timers.
    public sealed partial class AntVisual : MonoBehaviour
    {
        private Animator animator;
        private GameObject model;
        private LineRenderer ring;
        private SelectableObject selection;
        private SoldierAnt soldier;
        private WorkerAnt worker;
        private CommanderAnt commander;
        private WildMonster enemy;
        private Vector3 previousPosition;
        private float actionUntil;
        private string state;
        private MaterialPropertyBlock ringColor;
        private Color previousRingColor;
        private float previousRingWidth;
        private bool ringInitialized;
        private bool kemono;
        private string playedClip, heldTool;
        private KemonoAppearance appearance;
        private GameObject heldArrow;
        private MeshyEquipmentWearables equipmentWearables;
        public string StateName => state;
        public string MotionName => playedClip;
        public string HeldTool => heldTool;
        public int MotionRevision { get; private set; }
        public float MotionSpeed => animator ? animator.speed : 1f;
        public float MotionSpeedFor(float clipLength) => state == "Attack" && (worker == null || !worker.IsBuildingAnimation)
            ? clipLength / CombatMotionDuration : 1f;

        public static void Attach(GameObject owner)
        {
            if (owner.GetComponent<AntVisual>() == null && (KemonoModel.Available || Resources.Load<GameObject>("QuirkyAnt") != null))
                owner.AddComponent<AntVisual>();
        }

        private void Awake()
        {
            ringColor = new MaterialPropertyBlock();
            kemono = KemonoModel.Available;
            var prefab = kemono ? null : Resources.Load<GameObject>("QuirkyAnt");
            if (!kemono && prefab == null) { enabled = false; return; }
            foreach (var renderer in GetComponentsInChildren<MeshRenderer>()) renderer.enabled = false;
            model = kemono ? KemonoModel.Create(transform, Seed(name)) : Instantiate(prefab, transform, false);
            animator = RigAnimator.Find(model);
            appearance = model.GetComponent<KemonoAppearance>();
            if(kemono)equipmentWearables=model.AddComponent<MeshyEquipmentWearables>();
            animator.applyRootMotion = false;
            selection = GetComponent<SelectableObject>(); soldier = GetComponent<SoldierAnt>();
            worker = GetComponent<WorkerAnt>(); commander = GetComponent<CommanderAnt>(); enemy = GetComponent<WildMonster>();
            var ringObject = new GameObject("FactionRing"); ringObject.transform.SetParent(transform, false);
            ring = ringObject.AddComponent<LineRenderer>(); ring.useWorldSpace = false; ring.loop = true;
            ring.positionCount = 40; ring.widthMultiplier = .065f;
            ring.sharedMaterial = Resources.Load<Material>("AntSelection");
            for (var i = 0; i < ring.positionCount; i++)
            {
                var angle = i * Mathf.PI * 2 / ring.positionCount;
                ring.SetPosition(i, new Vector3(Mathf.Cos(angle), .06f, Mathf.Sin(angle)) * 1.1f);
            }
            previousPosition = transform.position;
            Play("Idle_A");
        }

        private void OnEnable() { previousPosition = transform.position; actionUntil = 0; state = null; }

        public void Action(string name, float duration = .35f)
        {
            if (animator == null || !isActiveAndEnabled) return;
            Play(name, true); actionUntil = Time.time + duration;
            if (name == "Hit" && model != null) AntColony.UI.Juice.Punch(model.transform); // 피격 움찔(손맛)
        }

        float CombatMotionDuration => commander?.Weapon != null && MeshyEquipmentRules.IsBow(commander.Weapon.weapon) ? 1.5f
            : commander?.Weapon != null && (commander.Weapon.weapon == WeaponKind.QuakeMaul || commander.Weapon.bossEquipment == BossEquipmentKind.AreaWeapon)
                ? Mathf.Clamp(commander.EffectiveAttackInterval * .85f, .45f, 1.5f) : .45f;

        public void Attack(Vector3 target)
        {
            Face(target - transform.position);
            float duration=kemono ? CombatMotionDuration : .45f;
            Action("Attack", duration);
            if(model!=null)model.GetComponent<MeshyEquipmentGrip>()?.BeginAttack(duration);
        }

        private void Face(Vector3 direction)
        {
            direction.y = 0;
            if (direction.sqrMagnitude > .0001f && model != null)
                model.transform.rotation = Quaternion.LookRotation(direction);
        }

        private void Play(string name, bool restart = false)
        {
            if (animator == null) return;
            var (clip, tool) = kemono ? Clip(name) : (name, null);
            if (!restart && state == name && playedClip == clip && heldTool == tool) return;
            var hash = Animator.StringToHash("Base Layer." + clip);
            if (!animator.HasState(0, hash)) return;
            animator.speed=kemono&&name=="Attack"&&(worker==null||!worker.IsBuildingAnimation)
                ?KemonoModel.AttackPlaybackSpeed(clip,CombatMotionDuration):1f;
            // 눕는 동작은 0.1초면 픽 쓰러지는 것처럼 보여 천천히 섞는다(2026-11-01 '캐릭터가 넘어진다').
            animator.CrossFadeInFixedTime(hash, name == "Sleep" || name == "Sit" ? .6f : .1f, 0); state = name; playedClip = clip;
            MotionRevision++;
            appearance?.FollowClip(clip);
            if(kemono)model.GetComponent<MeshyEquipmentGrip>().Motion=clip;
            if (kemono && heldTool != tool)
            {
                KemonoModel.Hold(model, tool); heldTool = tool; heldArrow = null;
                if (tool == "Weapon_ShortBow" || tool == "Weapon_GrandBow")
                    foreach (var t in model.GetComponentsInChildren<Transform>(true))
                        if (t.name.StartsWith("Ammo_Arrow") && t.gameObject.activeSelf) { heldArrow = t.gameObject; heldArrow.SetActive(false); break; }
            }
        }

        // 논리 상태 → kemono 클립·손 도구. 채집은 자원별 도구 동작, 건설은 망치, 전투는 검.
        private (string clip, string tool) Clip(string name)
        {
            if (name == "Work") return WorkClip();
            if (name == "Eat" && worker != null && worker.IsGatheringAnimation && worker.CurrentResourceNode != null)
            {
                if (worker.CurrentResourceNode.NeedsSowing) return ("Tool_Till", "Tool_Hoe_Stone");
                return KemonoModel.Gather(worker.CurrentResourceNode.ResourceType);
            }
            return name switch
            {
                "Idle_A" => ("IdleVariant"+(unchecked((uint)Seed(this.name))%3), null), "Hit" => ("Hit", null),
                "Sit" => (commander != null && commander.IsCaptive ? "Captive" : "Downed", null),
                "Attack" when worker != null && worker.IsBuildingAnimation => (worker.ConstructionClip, worker.ConstructionStep == 1 ? "Tool_Saw" : "Tool_Hammer_Stone"),
                "Attack" when commander != null && commander.Weapon != null && MeshyEquipmentRules.IsBow(commander.Weapon.weapon) => ("BowShoot", MeshyEquipmentRules.HeldModel(commander.Weapon)),
                "Attack" when commander != null && commander.Weapon?.weapon == WeaponKind.Shield => ("ShieldBlock", "Weapon_Shield_Copper"),
                "Attack" when commander != null && commander.Weapon == null => ("Attack_Unarmed", null),
                "Attack" when commander != null && commander.Weapon != null && (MeshyEquipmentRules.IsFirearm(commander.Weapon.weapon) || commander.Weapon.weapon == WeaponKind.AcidSprayer || commander.Weapon.weapon == WeaponKind.Pheromone) => ("Idle_A", MeshyEquipmentRules.HeldModel(commander.Weapon)),
                "Attack" when commander?.Weapon?.weapon == WeaponKind.Mandible => ("SpearThrust", MeshyEquipmentRules.HeldModel(commander.Weapon)),
                "Attack" when commander?.Weapon != null && (commander.Weapon.weapon == WeaponKind.QuakeMaul || commander.Weapon.bossEquipment == BossEquipmentKind.AreaWeapon) => ("Tool_Hammer", MeshyEquipmentRules.HeldModel(commander.Weapon)),
                "Attack" when commander != null => ("Attack", MeshyEquipmentRules.HeldModel(commander.Weapon)),
                "Attack" => ("Attack", "Weapon_Sword_Copper"),
                _ => (name, null)
            };
        }

        static int Seed(string text) { var h = 17; foreach (var c in text) h = h * 31 + c; return h; }

        private void LateUpdate()
        {
            if (model == null) return;
            AlignToGround();
            // Restored patients must already be lying down when a save is opened paused.
            if (commander != null && commander.TreatmentFacility != null && commander.TreatmentFacility.IsTreating(commander))
            { Play("Sleep"); previousPosition = transform.position; return; }
            if (Time.deltaTime <= 0) return;
            appearance?.SetArmor(commander != null && commander.EquippedArmor?.armor == ArmorKind.Coating);
            if(commander!=null&&equipmentWearables!=null)equipmentWearables.Apply(commander.EquippedArmor,
                commander.PersonalState.equipment.Find(e=>e.slot==EquipmentSlot.Trinket),commander.Weapon,heldTool,commander.PersonalHealth<=0);
            if (enemy == null && soldier == null) enemy = GetComponent<WildMonster>();
            if (commander != null && commander.IsEmbarked) { previousPosition = transform.position; return; }
            var delta = transform.position - previousPosition; previousPosition = transform.position;
            var moving = delta.sqrMagnitude > .000001f;
            var color = enemy != null ? new Color(1, .3f, .22f) : new Color(.3f, .85f, .65f);
            if (selection != null && selection.IsSelected) color = new Color(1, .9f, .35f);
            var width = selection != null && selection.IsSelected ? .13f : .055f;
            if (!ringInitialized || color != previousRingColor || width != previousRingWidth)
            {
                ring.startColor = ring.endColor = color;
                ringColor.SetColor("_BaseColor", color); ring.SetPropertyBlock(ringColor);
                ring.widthMultiplier = width;
                previousRingColor = color; previousRingWidth = width; ringInitialized = true;
            }
            if (heldArrow != null)
            {
                var bow = animator.GetCurrentAnimatorStateInfo(0);
                var frame = Mathf.Repeat(bow.normalizedTime, 1) * 36;
                heldArrow.SetActive(bow.IsName("Base Layer.BowShoot") && frame >= 9 && frame < 23);
            }
            if (Time.time < actionUntil) return;
            if (moving) Face(delta);
            if (commander != null && (commander.IsCaptive || commander.PersonalHealth <= 0)) Play("Sit");
            else if (commander != null && commander.IsAsleep && !moving) Play("Sleep");
            else if (commander != null && commander.IsEating) Play("Eat");
            else if (commander != null && commander.IsWashing) Play("Wash");
            else if (soldier != null && soldier.IsFlying || enemy != null && enemy.IsFlying) Play("Fly");
            else if (worker != null && worker.IsBuildingAnimation) { if (worker.ConstructionTarget != null) Face(worker.ConstructionTarget.Position - transform.position); Play("Attack"); }
            else if (worker != null && worker.IsGatheringAnimation)
            {
                if (worker.CurrentResourceNode != null) Face(worker.CurrentResourceNode.transform.position - transform.position);
                Play("Eat");
            }
            else if (moving) Play(CarryingPerson ? "CarryPerson" : worker != null && worker.IsCarrying ? "Carry" : commander!=null&&commander.PersonalHealth<35?"WalkInjured":delta.magnitude / Time.deltaTime > 2f ? "Run" : "Walk");
            else if (commander != null && commander.IsPlaying) Play(JoyClip());
            else if (HasStationaryWork) Play("Work");
            else Play("Idle_A");
        }

        public GameObject Death(bool persistent = false)
        {
            if (model == null) return null;
            var native=GetComponent<MeshyNativeMotionPlayer>();
            if(native){var nativeBody=native.CreateDeath(persistent);if(nativeBody)return nativeBody;}
            var body = Instantiate(model, model.transform.position, model.transform.rotation);
            foreach(var renderer in body.GetComponentsInChildren<Renderer>(true))renderer.forceRenderingOff=false;
            body.transform.localScale = model.transform.lossyScale;
            RigAnimator.Find(body).Play("Base Layer.Death", 0, 0);
            if (kemono) KemonoModel.Hold(body, null);
            body.GetComponent<KemonoAppearance>()?.SetExpression("DEAD");
            if (!persistent) Destroy(body, 2.5f);
            return body;
        }
    }
}
