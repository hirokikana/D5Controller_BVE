using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.DirectX.DirectInput;
using System.Runtime.InteropServices;
using System.Diagnostics;



namespace WindowsFormsApplication1
{
    public partial class main : Form
    {
        private Device joystick = null;

        private int current_mascon_state = 0;
        private int current_brake_state = 9;


        public main()
        {
            InitializeComponent();

        }


        private void joystick_initilize()
        {
            //create joystick device. 
            foreach (
                DeviceInstance di in
                Manager.GetDevices(
                    DeviceClass.GameControl,
                    EnumDevicesFlags.AttachedOnly))
            {
                joystick = new Device(di.InstanceGuid);
                break;
            }

            if (joystick == null)
            {
                //Throw exception if joystick not found. 
                throw new Exception("No joystick found.");
            }

            //Set joystick axis ranges. 
            foreach (DeviceObjectInstance doi in joystick.Objects)
            {
                if ((doi.ObjectId & (int)DeviceObjectTypeFlags.Axis) != 0)
                {
                    joystick.Properties.SetRange(
                        ParameterHow.ById,
                        doi.ObjectId,
                        new InputRange(-5000, 5000));
                }
            }

            //Set joystick axis mode absolute. 
            joystick.Properties.AxisModeAbsolute = true;

            //set cooperative level. 
            joystick.SetCooperativeLevel(
                this,
                CooperativeLevelFlags.NonExclusive |
                CooperativeLevelFlags.Background);

            //Acquire devices for capturing. 
            joystick.Acquire(); 
        }

        private int get_mascon_state(int x, int b1)
        {
            if (x == -40 && b1 == 128)
            {
                return 5;
            }

            if (x == 5000)
            {
                return b1 == 128 ? 1 : 2;                
            }
            else if (x == -5000)
            {
                return b1 == 128 ? 3 : 4;
            }

            if (x == 0 && b1 == 0)
            {
                return 0;
            }
            return -1;
        }

        [DllImport("user32.dll")]
        static extern int SendInput(int nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        static extern IntPtr GetMessageExtraInfo();

        [DllImport("user32.dll")]
        static extern int MapVirtualKey(int uCode, int uMapType);

        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }


        [StructLayout(LayoutKind.Explicit)]
        private struct INPUT
        {
            [FieldOffset(0)]
            public uint type;
            [FieldOffset(4)]
            public MOUSEINPUT mi;
            [FieldOffset(4)]
            public KEYBDINPUT ki;
            [FieldOffset(4)]
            public HARDWAREINPUT hi;
        }
        const int INPUT_KEYBOARD = 1;
        const int KEYEVENTF_EXTENDEDKEY = 0x1;
        const int KEYEVENTF_KEYUP = 0x2;
        const int KEYEVENTF_SCANCODE = 0x8;
        const int KEYEVENTF_UNICODE = 0x4;
        // 拡張キーと制御コードのチェックはしない。
        private static INPUT[] ToInput(string key_code_str)
        {
            ushort key_code = Convert.ToUInt16(key_code_str, 16);
            INPUT[] input = new INPUT[2];
            System.Threading.Thread.Sleep(10);
            input[0].type = INPUT_KEYBOARD;
            input[0].ki.wVk = key_code;
            input[0].ki.wScan = (ushort)(MapVirtualKey(key_code,0));
            input[0].ki.dwFlags = KEYEVENTF_SCANCODE;
            SendInput(1, input, Marshal.SizeOf(typeof(INPUT)));
            System.Threading.Thread.Sleep(10);
            input[0] = input[0];
            input[0].ki.dwFlags |= KEYEVENTF_KEYUP;
            SendInput(1, input, Marshal.SizeOf(typeof(INPUT)));
            
            return input;
        }

        private void send_mascon_key(int control_count) {
            // マイナスだったらAキーを指定回数、プラスだったらZキーを指定回数
            string mascon_key = control_count < 0 ? "41" : "5A";
            for (int i = 0; i < Math.Abs(control_count); i++)
            {
                INPUT[] inputs = ToInput(mascon_key);
                //SendInput(inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
                
            }
        }
        private void send_brake_key(int control_count)
        {
            // マイナスだったら,キーを指定回数、プラスだったら.キーを指定回数
            string mascon_key = control_count < 0 ? "BC" : "BE";
            for (int i = 0; i < Math.Abs(control_count); i++)
            {
                INPUT[] inputs = ToInput(mascon_key);
                //SendInput(inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));

            }
        }

        private int get_brake_state(int b5, int b6, int b7, int b8)
        {
            if (b5 == 128 && b6 == 128 && b7 == 128 && b8 == 128)
            {
                return -1;
            }
            if (b5 == 0 && b6 == 0 && b7 == 0 && b8 == 0)
            {
                return 9;
            }
            if (b6 == 0 && b8 == 128)
            {
                return b7 == 0 ? 8 : 7;
            }else
            {
                if (b8 == 128) {
                    if (b5 == 128 && b6 == 128) return 0;
                    return b7 == 0 ? 2 : 1;
                }else{
                    if (b5 == 128 && b6 == 128 && b7 == 128) return 3;
                    if (b5 == 128 && b6 == 128) return 4;
                    if (b7 == 0) return 6;
                    if (b7 == 128) return 5;
                    return -2;
                }
            } 

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            // デバイス未決定時はinitilizeする
            if (joystick == null)
            {
                try
                {
                    joystick_initilize();
                }
                catch (Exception)
                {
                    label1.Text = "ジョイスティックを認識できませんでした";
                    label2.Text = "";
                }
            }
            
            try
            {
                // コントローラの状態をポーリングで取得
                joystick.Poll();
                JoystickState state = joystick.CurrentJoystickState;

                //-----------------------
                // ボタンの状態を出力
                //-----------------------
                int count = 0;
                List<int> button_state = new List<int>();
                foreach (byte button in state.GetButtons())
                {
                    if (count++ >= joystick.Caps.NumberButtons)
                    {
                        break;  // ボタンの数分だけ状態を取得したら終了
                    }
                    button_state.Add(button);
                }

                int b1 = button_state[0];
                
                //マスコン
                int mascon_state = get_mascon_state(state.X, b1);
                label1.Text = "マスコン：" + mascon_state;
                if (current_mascon_state != mascon_state && mascon_state != -1)
                {
                    send_mascon_key(mascon_state - current_mascon_state);                   
                    current_mascon_state = mascon_state;
                }

                //ブレーキ
                int brake_state = get_brake_state(button_state[4], button_state[5], button_state[6], button_state[7]);
                label2.Text = "ブレーキ：" + brake_state;
                if (current_brake_state != brake_state && brake_state != -1 && brake_state != 9)
                {
                    int max_brake = 8;
                    if (brake_state <= max_brake)
                    {
                        send_brake_key(brake_state - current_brake_state);
                    }
                    current_brake_state = brake_state;
                }
                else if (brake_state == 9 && current_brake_state != brake_state)
                {
                    ToInput("BF");
                    current_brake_state = brake_state;
                }

            }
            catch (Exception ex)
            {
            }     

        }
    }
}
