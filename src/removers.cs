using Fahrenheit.CoreLib.FFX.Atel;
using System.Runtime.InteropServices;

using static Fahrenheit.Modules.CSR.CSRModule;

namespace Fahrenheit.Modules.CSR;

internal static unsafe partial class Removers {
    [LibraryImport("msvcrt.dll", EntryPoint = "memset", SetLastError = false)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    private static partial isize memset(isize dst, i32 value, isize size);

    public static void init() {
        removers.Add("znkd0600", Zanarkand.remove_intro);
        removers.Add("znkd1000", Zanarkand.remove_prelude);

        removers.Add("bsil0300", Besaid.remove_valley);
        removers.Add("bsil0600", Besaid.remove_promontory);
        removers.Add("bsil0700", Besaid.remove_village_slope);
    }

    private static void remove(u8* code_ptr, i32 from, i32 to) {
        memset((isize)code_ptr + from, 0, to - from);
    }

    private static void set(u8* code_ptr, i32 offset, u8 value) {
        *(code_ptr + offset) = value;
    }

    private static void set(u8* code_ptr, i32 offset, AtelOpCode opcode) {
        u8* ptr = code_ptr + offset;
        foreach (u8 b in opcode.to_bytes()) {
            *ptr = b;
            ptr++;
        }
    }

    private static void set_tp(u8* code_ptr, i32 offset, u16 x_idx, u16 y_idx, u16 z_idx) {
        u8* ptr = code_ptr + offset;
        set(ptr, 0x0, AtelInst.PUSHF.build(x_idx));
        set(ptr, 0x3, AtelInst.PUSHF.build(y_idx));
        set(ptr, 0x6, AtelInst.PUSHF.build(z_idx));
        set(ptr, 0x9, AtelInst.CALLPOPA.build(0x126));
    }

    private static class Besaid {
        public static void remove_valley(u8* code_ptr) {
            remove(code_ptr, 0x1B29, 0x1C43); // Wakka pushes Tidus into the water

            remove(code_ptr, 0x3398, 0x33C6); // Initial fadeout into cutscene
            remove(code_ptr, 0x1CFC, 0x1FCF); // Wakka asks Tidus to join the Aurochs

            // Skip the Promontory since it'd be instantly skipped anyway
            // We essentially copy bsil0600:3EB3..3EC8 to bsil0300:1FCF..1FEA
            set(code_ptr, 0x1FCF, AtelInst.PUSHII.build(0x7E)); // GameMoment = 124 -> GameMoment = 126
            set(code_ptr, 0x1FD5, AtelInst.PUSHII.build(0x0F)); // Common.00BB(0) -> Common.00BB(15)
            set(code_ptr, 0x1FE1, AtelInst.PUSHII.build(69));
            set(code_ptr, 0x1FE7, AtelInst.CALL.build(0x11)); // Common.010C(67, 0) -> Common.transitionToMap(69, 0)
        }

        public static void remove_promontory(u8* code_ptr) {
            remove(code_ptr, 0x3DB4, 0x3EAD); // Cutscene coming from Valley DEPRECATED
        }

        public static void remove_village_slope(u8* code_ptr) {
            remove(code_ptr, 0x264D, 0x289A); // First cutscene

            remove(code_ptr, 0x28B7, 0x28C0); // Don't make the game wait for a fade that won't happen

            set_tp(code_ptr, 0x264D, 0xD, 0xE, 0xF); // Set player position to the vanilla post-cutscene one
        }
    }

    private static class Zanarkand {
        public static void remove_intro(u8* code_ptr) {
            // We basically write our own script here
            set(code_ptr, 0x3DC4, AtelInst.PUSHV.build(0x0));
            set(code_ptr, 0x3DC7, AtelInst.PUSHII.build(0x2));
            set(code_ptr, 0x3DCA, AtelInst.LS.build());           // if (GameMoment < 2)
            set(code_ptr, 0x3DCB, AtelInst.POPXCJMP.build(0x13)); // goto intro_exit
                                                                  // else
            set(code_ptr, 0x3DCE, AtelInst.JMP.build(0xE));       // goto midgame_exit
            set(code_ptr, 0x3DD1, AtelInst.RET.build()); // return; // not actually necessary?

            // Set the story progress higher than normal to skip the prelude camera pan
            set(code_ptr, 0x4450, AtelInst.PUSHII.build(3)); // GameMoment = 2 -> 3

            // Remove some hardcoded wait time
            remove(code_ptr, 0x43D3, 0x440A); // midgame
            remove(code_ptr, 0x4426, 0x4450); // intro
        }

        public static void remove_prelude(u8* code_ptr) {
            // The below code *works*, but it causes the game to think Tidus needs constant affirmation of his name
            // No, little child, I have a whale to sashimi
            //remove(code_ptr, 0x6A35, 0x6A9D); // Skips until running w00e10 (in w1Dtalk)
            //remove(code_ptr, 0x6AA8, 0x6AE8); // Skips after running w00e10 (in w1Dtalk)
            //remove(code_ptr, 0x18D, 0x23A);             // Skips until naming Tidus (in w00e10)
            //set(code_ptr, 0x240, AtelInst.RET.build()); // Skips after naming Tidus (in w00e10)

            // Begone, thot
            //remove(code_ptr, 0x6B36, 0x6BEF); // Skips until running w00e12 (in w1Etalk)
            //remove(code_ptr, 0x6BFA, 0x6C7A); // Skips after running w00e12 (in w1Etalk)
            //remove(code_ptr, 0x48E, 0x54A);             // Skips until naming Tidus (in w00e12)
            //set(code_ptr, 0x550, AtelInst.RET.build()); // Skips after naming Tidus (in w00e12)
        }
    }
}
