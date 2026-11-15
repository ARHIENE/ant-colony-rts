using System;
using System.Linq;
using AntColony.Core;
using AntColony.Data;
using AntColony.UI;
using UnityEngine;

namespace AntColony.Units
{
    [Serializable] public sealed class CommanderAlcoholState
    {
        public bool allowed, seeking, forced, returnDraft;
        public int intervalDays = 2, portion = -1, binges;
        public float intoxication, hangover, sinceDrink, drinking, retry, searchSeconds;
        public Vector3 returnPosition;
        public bool Valid => intervalDays >= 1 && intervalDays <= 7 && binges >= 0 && binges <= 6
            && (portion == -1 || Enum.IsDefined(typeof(ResourceType), portion) && ProcessingResources.Alcohol((ResourceType)portion))
            && new[]{intoxication,hangover,sinceDrink,drinking,retry,searchSeconds,returnPosition.x,returnPosition.y,returnPosition.z}.All(v=>!float.IsNaN(v)&&!float.IsInfinity(v))
            && intoxication >= 0 && intoxication <= 120 && hangover >= 0 && hangover <= GameCalendar.SecondsPerDay*2
            && sinceDrink >= 0 && sinceDrink <= GameCalendar.SecondsPerDay*30 && drinking >= 0 && drinking <= 5 && retry >= 0 && searchSeconds >= 0;
    }
    public partial class CommanderAnt
    {
        public CommanderAlcoholState Alcohol => personalState.alcohol ??= new CommanderAlcoholState();
        public bool AlcoholDependent => traits.Has(CommanderTrait.AlcoholDependent);
        public bool AlcoholWithdrawal => AlcoholDependent && Alcohol.sinceDrink >= GameCalendar.SecondsPerDay*2;
        public bool AlcoholBlackout => Alcohol.intoxication >= 100;
        public bool IsDrinking => Alcohol.portion >= 0;
        public bool SeekingDrink => Alcohol.seeking || IsDrinking;
        public float AlcoholFactor => AlcoholBlackout ? 0 : Mathf.Clamp(1-Alcohol.intoxication*.004f-(Alcohol.hangover>0?.15f:0)-(AlcoholWithdrawal?.15f:0),.25f,1);
        public string AlcoholStatus => (AlcoholBlackout ? "만취 · 행동 불가" : IsDrinking ? "음주 중" : Alcohol.seeking ? "술을 가지러 이동 중" : Alcohol.intoxication>0 ? $"취기 {Alcohol.intoxication:0}%" : "맑은 정신")
            +(Alcohol.hangover>0 ? $" · 숙취 {Alcohol.hangover:0}초" : "")+(AlcoholWithdrawal ? " · 금단" : "");
        private void TickAlcohol(float seconds)
        {
            var a=Alcohol;
            a.sinceDrink=Mathf.Min(GameCalendar.SecondsPerDay*30,a.sinceDrink+seconds);
            a.retry=Mathf.Max(0,a.retry-seconds);
            a.hangover=Mathf.Max(0,a.hangover-seconds);
            a.intoxication=Mathf.Max(0,a.intoxication-seconds*(40f/GameCalendar.SecondsPerDay));
            if(a.hangover>0) personalState.AddMood("AlcoholHangover",-5,2); else personalState.moodFactors.RemoveAll(f=>f.reason=="AlcoholHangover");
            if(AlcoholWithdrawal)personalState.AddMood("AlcoholWithdrawal",-10,2); else personalState.moodFactors.RemoveAll(f=>f.reason=="AlcoholWithdrawal");
            if(AlcoholBlackout){ if(SeekingDrink) CancelDrink(); if(Agent!=null&&Agent.isOnNavMesh)Agent.ResetPath(); }
        }
        public string DrinkBlock(bool forced=false)
        {
            if(IsChild)return "어린 장수는 음주 불가";
            if(IsAwayFromHome||IsCaptive||IsEmbarked||IsDead||!CanReceiveOrders||IsAsleep||IsEating||IsWashing||LabUpgradeBusy)return "현재 상태에서는 음주 불가";
            if(!forced && (!Alcohol.allowed || IsDeployed || WorkPriorities.Red || IsInCombat || HasCombatOrder || IsDirectMoving && !SeekingDrink || AntColony.World.WaveDefense.Instance?.InWave==true))return "정책 금지 또는 전투 대기";
            if(!forced && Alcohol.sinceDrink < Alcohol.intervalDays*GameCalendar.SecondsPerDay)return "음주 간격 대기";
            if(Alcohol.intoxication>=60 && !forced)return "취기가 가신 뒤 음주 가능";
            if(!forced && (IsCarrying||IsConstructing||CraftingWorkshop!=null))return "현재 작업 완료 후 음주 가능";
            return "";
        }
        private StoreBin DrinkSource(out ResourceType type)
        {
            type=default;var rm=ResourceManager.Instance;if(rm==null)return null;
            foreach(var bin in rm.Bins.Where(b=>b.Spendable).OrderBy(b=>(b.Position-Position).sqrMagnitude))
                foreach(var drink in new[]{ResourceType.Beer,ResourceType.RiceWine,ResourceType.FruitWine,ResourceType.Mead,ResourceType.HoneydewWine,ResourceType.Spirits})
                    if(bin.Amount(drink)>0&&TryWorkApproach(bin.Position,out _)){type=drink;return bin;}
            return null;
        }
        public bool RequestDrink()
        {
            if(DrinkBlock()!=""||SeekingDrink||DrinkSource(out _)==null)return false;
            BeginDrink(false);return true;
        }
        private IDamageable drinkReturnTarget;
        private void BeginDrink(bool forced)
        {
            var deployed=IsDeployed;var origin=Position;var target=CombatTarget;
            if(forced && IsCarrying) DropCargo();
            SuspendForNeeds("drink");LeavePlay();
            var a=Alcohol;a.seeking=true;a.forced=forced;a.returnDraft=deployed;a.returnPosition=origin;a.searchSeconds=0;
            drinkReturnTarget=target;
            if(forced)ToastManager.Show(CommanderName+": 알코올 의존 충동으로 술을 찾으러 이탈합니다.");
        }
        private bool TickDrinkAction(float seconds,bool forcedOnly)
        {
            var a=Alcohol;
            if(forcedOnly && !a.forced && !AlcoholWithdrawal)return false;
            if(!SeekingDrink)
            {
                if(a.retry>0 || !forcedOnly && Joy>GameBalance.PlayBelowJoy)return false;
                bool forced=AlcoholWithdrawal;
                if(DrinkBlock(forced)!="")return false;
                if(DrinkSource(out _)==null){a.retry=10;return false;}
                BeginDrink(forced);
            }
            if(DrinkBlock(a.forced)!=""){CancelDrink();return false;}
            if(a.portion<0)
            {
                a.searchSeconds+=seconds;
                var bin=DrinkSource(out var type);
                if(bin==null || a.searchSeconds>60){CancelDrink();a.retry=10;return false;}
                if(!TryWorkApproach(bin.Position,out var approach)){CancelDrink();return false;}
                if((Position-approach).sqrMagnitude>2.25f)
                {if(IsFlying||Agent!=null&&!Agent.pathPending&&!Agent.hasPath)base.CommandMove(approach);return true;}
                if(ResourceManager.Instance.TakeFrom(bin,type,1)!=1)return true;
                a.portion=(int)type;a.drinking=5;a.seeking=false;
                if(Agent!=null&&Agent.isOnNavMesh)Agent.ResetPath();
            }
            a.drinking=Mathf.Max(0,a.drinking-seconds);
            if(a.drinking>0)return true;
            var kind=(ResourceType)a.portion;
            float dose=(kind==ResourceType.Spirits?55:25)*(traits.Has(CommanderTrait.AlcoholWeak)?1.6f:traits.Has(CommanderTrait.AlcoholStrong)?.65f:1);
            a.intoxication=Mathf.Min(120,a.intoxication+dose);a.sinceDrink=0;
            if(a.intoxication>=60)
            {
                a.hangover=GameCalendar.SecondsPerDay;a.binges=Mathf.Min(6,a.binges+1);
                if(a.binges>=6&&!AlcoholDependent&&traits.TryAdd(CommanderTrait.AlcoholDependent))ToastManager.Show(CommanderName+": 반복 과음으로 알코올 의존이 생겼습니다.");
            }
            personalState.AddMood("AlcoholRelaxed",5,GameCalendar.SecondsPerDay*.5f);personalState.moodFactors.RemoveAll(f=>f.reason=="AlcoholWithdrawal");
            JoyState.joy=Mathf.Min(100,JoyState.joy+20);
            a.portion=-1;a.forced=false;a.retry=10;ResumeAfterDrink();return true;
        }
        private void ResumeAfterDrink()
        {
            var a=Alcohol;
            if(a.returnDraft&&IsDeployed&&!AlcoholBlackout)
            {if(drinkReturnTarget!=null&&!drinkReturnTarget.IsDead)CommandAttack(drinkReturnTarget);else CommandAttackMove(a.returnPosition);}
            a.returnDraft=false;drinkReturnTarget=null;
        }
        private void CancelDrink()
        {
            var a=Alcohol;
            if(a.portion>=0)AntColony.World.DiplomacyManager.DropFloor((ResourceType)a.portion,1,Position);
            a.portion=-1;a.drinking=0;a.seeking=false;a.forced=false;a.searchSeconds=0;
            a.returnDraft=false;drinkReturnTarget=null;
        }
    }
}
