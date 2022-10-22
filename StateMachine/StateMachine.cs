using HarmonyLib;
using System;
using System.Collections.Generic;

namespace PlanetWormhole.StateMachine
{
    public class StateMachine
    {
        private int n;
        private CodeInstruction[] circle;
        private Func<CodeInstruction, bool>[] conditions;
        private int state;
        private CodeInstruction[] replaced;

        public StateMachine(int n, CodeInstruction[] replaced
            , Func<CodeInstruction, bool>[] conditions)
        {
            this.n = n;
            this.replaced = replaced;
            this.conditions = conditions;
            circle = new CodeInstruction[n];
        }

        public IEnumerable<CodeInstruction> Replace(IEnumerable<CodeInstruction> instructions)
        {
            state = 0;
            foreach (CodeInstruction instruction in instructions)
            {
                if (conditions[state].Invoke(instruction))
                {
                    state++;
                    if (state == n)
                    {
                        foreach (CodeInstruction code in replaced)
                        {
                            yield return code;
                        }
                        state = 0;
                    }
                    else
                    {
                        circle[state - 1] = instruction;
                    }
                }
                else
                {
                    for (int i = 0; i < state; i++)
                    {
                        yield return circle[i];
                    }
                    yield return instruction;
                    state = 0;
                }
            }
        }
    }
}
