using BepInEx;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UK_PCSR;
using UnityEngine;
using SystemReflectionOpCodes = System.Reflection.Emit.OpCodes;
using System.Reflection.Emit;
using MonoCecilOpCodes = Mono.Cecil.Cil.OpCodes;
using CodeInstruction = HarmonyLib.CodeInstruction;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;



namespace UK_PCSR
{
    [BepInPlugin("com.awesomesauce.UltraKillUK_ParryClankReplacer", "UK_ParryClankReplacer", "1.0.0")]
    public class ParryReplacer : BaseUnityPlugin
    {
        public static AudioSource ParrySource;
        public static AudioClip ParryClip;
        public static Harmony harmony;
        private void Awake()
        {
            // Plugin startup logic 
            Logger.LogInfo($"Sugarcoating disabled.");
            ParrySource = Assets.AudioParrySource;
            ParryClip = Assets.AudioParryClip;
            harmony = new Harmony("com.awesomesauce.UltraKillUK_ParryClankReplacer");
            harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(Punch))]
    [HarmonyPatch("Start")]
    public class ParryPatch
    {
        private static void Postfix(Punch __instance)
        {
            __instance.specialHit.clip = ParryReplacer.ParryClip;
        }
    }
    /*[HarmonyPatch(typeof(Punch))]
    [HarmonyPatch("Parry")]
    public class ParryPatch
    {
        private static void Postfix(Punch __instance, ref AudioSource ___aud)
        {
            ___aud.clip = ParryReplacer.ParryClip;
        }
    }*/

    public static class SoundConverter
    {
        public static int FindBitDepth(byte[] ByteArray)
        {
            byte[] WorkingArray = new byte[2];
            for (int i = 0; i < 2; i++)
            {

                byte WorkingByte = ByteArray[i + 34];
                WorkingArray[i] = WorkingByte;
                WorkingArray[WorkingArray.Length - 1] = WorkingByte;
            }
            int CombinedBytes = BitConverter.ToInt16(WorkingArray, 0);
            return CombinedBytes;
        }
        public static float[] ConvertByteToFloat(byte[] byteArray)
        {
            const int headerSize = 44; // WAV header size
            int floatCount = (byteArray.Length - headerSize) / sizeof(short); // Assuming 16-bit PCM
            float[] floatArray = new float[floatCount];
            int BitDepth = FindBitDepth(byteArray);
            //Console.WriteLine(BitDepth);
            if (BitDepth == 16)
            {
                for (int i = 0; i < floatCount; i++)
                {
                    short sample = BitConverter.ToInt16(byteArray, headerSize + i * sizeof(short));
                    floatArray[i] = sample / 32768f; // Convert to float
                }
            }
            else if (BitDepth == 32)
            {
                for (int i = 0; i < floatCount; i++)
                {
                    int sample = BitConverter.ToInt32(byteArray, headerSize + i * sizeof(int));
                    floatArray[i] = sample / 32768f; // Convert to float
                }
            }

            return floatArray;
        }
        public static int FindFrequency(byte[] ByteArray)
        {
            byte[] WorkingArray = new byte[4];
            for (int i = 0; i < 3; i++)
            {
                byte WorkingByte = ByteArray[i + 24];
                WorkingArray[i] = WorkingByte;
                WorkingArray[WorkingArray.Length - 1] = WorkingByte;
            }
            int CombinedBytes = BitConverter.ToInt32(WorkingArray, 0);
            //Console.WriteLine(CombinedBytes);
            return CombinedBytes * 2; //audio plays slow, brute forcing frequency higher fixes
        }

    }
    public static class Assets
    {
        public static AudioSource AudioParrySource;
        public static GameObject AudioClipObject;
        static string ModDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static string AudioPath = Path.Combine(ModDir, "ParryNoise");
        static byte[] RawAudio = File.ReadAllBytes(Path.Combine(AudioPath, "parry.wav"));

        static float[] FloatArray = SoundConverter.ConvertByteToFloat(RawAudio);
        static int Frequency = SoundConverter.FindFrequency(RawAudio);

        public static AudioClip AudioParryClip = AudioClip.Create("ParryNoise", FloatArray.Length, 1, Frequency, false);
        static Assets()
        {
            AudioParryClip.SetData(FloatArray, 0);
            AudioClipObject = new GameObject("AudioClipObject");

            AudioParrySource = AudioClipObject.AddComponent<AudioSource>();
            AudioParrySource.playOnAwake = true;
            AudioParrySource.spatialBlend = 0f; // Set to 0 for 2D sound
            AudioParrySource.volume = 1f; // Set volume level as needed

        }
    }


        //TODO: FIX
        /*static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);
            var targetField = AccessTools.Field(typeof(Punch), "specialHit");
            var replacementField = AccessTools.Field(typeof(Assets), "AudioParrySource");

            for (int i = 0; i < code.Count; i++)
            {
                if (code[i].opcode == SystemReflectionOpCodes.Ldfld)
                {
                    Debug.LogWarning(code[i].ToString() + " " + i);
                }
                else
                {
                    Debug.Log(code[i].ToString() + " " + i);
                }
                    
                // Check for the instruction that loads `Punch.specialHit`
                if (code[i].opcode == SystemReflectionOpCodes.Ldfld && code[i].operand is FieldInfo field && field == targetField)
                {
                    // Replace with the instruction to load `Assets.AudioParrySource`
                    code[i].opcode = SystemReflectionOpCodes.Ldsfld;
                    code[i].operand = replacementField;
                    Debug.Log("Replaced field loading instruction");
                    Debug.Log("New instruction: " + code[i].ToString());
                }
            }

            return code.AsEnumerable();
        }*/

    

    
}