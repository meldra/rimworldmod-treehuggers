namespace Treehuggers
{

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Reflection;
    using RimWorld;
    using HarmonyLib;
    using Verse;

    [DefOf]
    public static class MyDefOf
    {
        public static TraitDef Vegan;
        public static TraitDef Vegetarian;
        public static ThoughtDef AteMeatTreehugger;
        public static ThoughtDef AteAnimalProductTreehugger;
        public static ThoughtDef HurtAnAnimalTreehugger;
        public static ThingCategoryDef AnimalProductRaw = DefDatabase<ThingCategoryDef>.GetNamed("AnimalProductRaw");
        //public static ThingCategoryDef AllAnimals = DefDatabase<Thing>.GetNamed("Animal");

        static MyDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(MyDefOf));
        }
    }

    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        private static readonly Type patchType = typeof(HarmonyPatches);

        static HarmonyPatches()
        {
            Harmony harmony = new Harmony(id: "rimworld.elkyelky.Treehuggers");
            harmony.Patch(original: AccessTools.Method(type: typeof(FoodUtility), name: nameof(FoodUtility.ThoughtsFromIngesting)), prefix: null,
                 postfix: new HarmonyMethod(methodType: patchType, methodName: nameof(AddTreehuggerRawFoodThoughts_Postfix)));

            harmony.Patch(original: AccessTools.Method(type: typeof(FoodUtility), name: "AddIngestThoughtsFromIngredient"), prefix: null,
                postfix: new HarmonyMethod(methodType: patchType, methodName: nameof(AddTreehuggerMealFoodThoughts_Postfix)));

            harmony.Patch(original: AccessTools.Method(type: typeof(Thing), name: nameof(Thing.TakeDamage)), prefix: null,
                postfix: new HarmonyMethod(methodType: patchType, methodName: nameof(AddTreehuggerHurtAnimalMemory_Postfix)));

            harmony.PatchAll();
        }

        public static void AddTreehuggerRawFoodThoughts_Postfix(Pawn ingester, Thing foodSource, ThingDef foodDef, ref List<ThoughtDef> ___ingestThoughts)
        {
            if (foodDef.IsMeat
            && ingester.RaceProps.Humanlike
            && (ingester.story.traits.HasTrait(MyDefOf.Vegetarian) || ingester.story.traits.HasTrait(MyDefOf.Vegan)))
            {
                ___ingestThoughts.Add(MyDefOf.AteMeatTreehugger);
            }
            else if (foodDef.thingCategories.Contains(MyDefOf.AnimalProductRaw)
            && ingester.RaceProps.Humanlike
            && ingester.story.traits.HasTrait(MyDefOf.Vegan))
            {
                ___ingestThoughts.Add(MyDefOf.AteAnimalProductTreehugger);
            }
        }

        public static void AddTreehuggerMealFoodThoughts_Postfix(ThingDef ingredient, Pawn ingester, List<ThoughtDef> ingestThoughts)
        {
            if (ingredient.IsMeat
            && ingester.RaceProps.Humanlike
            && (ingester.story.traits.HasTrait(MyDefOf.Vegetarian) || ingester.story.traits.HasTrait(MyDefOf.Vegan)))
            {
                ingestThoughts.Add(MyDefOf.AteMeatTreehugger);
            }
            else if (!ingredient.thingCategories.Contains(MyDefOf.AnimalProductRaw)
            && ingester.RaceProps.Humanlike
            && ingester.story.traits.HasTrait(MyDefOf.Vegan))
            {
                ingestThoughts.Add(MyDefOf.AteAnimalProductTreehugger);
            }
        }

        public static void AddTreehuggerHurtAnimalMemory_Postfix(Thing __instance, DamageInfo dinfo)
        {
            Pawn attacker = dinfo.Instigator as Pawn;
            Pawn victim = __instance as Pawn;
            if (attacker == null || victim == null) return;
            if (attacker.RaceProps.Humanlike
            && !victim.RaceProps.Humanlike
            && !victim.RaceProps.IsMechanoid
            && (attacker.story.traits.HasTrait(MyDefOf.Vegan) || attacker.story.traits.HasTrait(MyDefOf.Vegetarian)))
            {
                attacker.needs.mood.thoughts.memories.TryGainMemory(MyDefOf.HurtAnAnimalTreehugger, victim);
            }
        }
    }

    public class ThoughtWorker_AnyWoolApparel : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            string text = null;
            int num = 0;
            List<Apparel> wornApparel = p.apparel.WornApparel;
            for (int i = 0; i < wornApparel.Count; i++)
            {
                if (wornApparel[i].Stuff != null
                && wornApparel[i].Stuff.defName.Contains("Wool"))
                {
                    if (text == null)
                    {
                        text = wornApparel[i].def.label;
                    }
                    num++;
                }
            }
            if (num == 0)
            {
                return ThoughtState.Inactive;
            }
            if (num >= 5)
            {
                return ThoughtState.ActiveAtStage(4, text);
            }
            return ThoughtState.ActiveAtStage(num - 1, text);
        }
    }

    public class ThoughtWorker_AnyLeatherApparel : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            string text = null;
            int num = 0;
            List<Apparel> wornApparel = p.apparel.WornApparel;
            for (int i = 0; i < wornApparel.Count; i++)
            {
                if (wornApparel[i].Stuff != null
                && wornApparel[i].Stuff.IsLeather)
                {
                    if (text == null)
                    {
                        text = wornApparel[i].def.label;
                    }
                    num++;
                }
            }
            if (num == 0)
            {
                return ThoughtState.Inactive;
            }
            if (num >= 5)
            {
                return ThoughtState.ActiveAtStage(4, text);
            }
            return ThoughtState.ActiveAtStage(num - 1, text);
        }
    }
}
