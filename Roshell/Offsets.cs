using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobloxConsoleExplorer
{
    public class Offsets
    {
        public static int FakeDataModel = 0x6ED6E38;
        public static int FakeToRealDataModel = 0x1C0;

        public static int Name = 0xB0;
        public static int Children = 0x70;
        public static int Parent = 0x68;
        public static int LocalPlayer = 0x130;
        public static int ModelInstance = 0x380;

        public static int LocalScriptBytecode = 0x1A8;
        public static int ModuleScriptBytecode = 0x150;

        public static int Text = 0xE00;
        public static int GuiSize = 0x4E0; //int, float, int, float
        public static int BackgroundColor3 = 0x4F0;
        public static int GuiVisible = 0x559;
        public static int GuiRotation = 0x544;
        public static int GuiPosition = 0x4C0;
    }
}
