using System;
using System.Diagnostics.Contracts;
using System.Drawing;

namespace TrackballScroll
{
    abstract class State
    {
        public enum CallNextHook
        {
            FALSE,
            TRUE
        }

        protected static bool toggleScroll = false;

        public class Result
        {
            public State NextState { get; }
            public CallNextHook CallNextHook { get; }
            public WinAPI.INPUT[] Input { get; }

            public Result(State nextState)
                : this(nextState, CallNextHook.TRUE, null)
            { }

            public Result(State nextState, CallNextHook callNextHook, WinAPI.INPUT[] input)
            {
                NextState = nextState;
                CallNextHook = callNextHook;
                Input = input;
            }
        }

        [Pure]
        public abstract Result Process(IntPtr wParam, WinAPI.MSLLHOOKSTRUCT llHookStruct, Properties.Settings settings);

        // Instead of handling different scaling values on multiple monitors, they both position variants are stored independantly.
        public WinAPI.POINT Origin { get; } // Origin contains original screen resolution values as reported by the event message.

        protected State(WinAPI.POINT origin)
        {
            Origin = origin;
        }

        [Pure]
        protected uint GetXButton(uint llHookStructMouseData)
        {
            return (llHookStructMouseData & 0xFFFF0000) >> 16; // see https://docs.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-msllhookstruct?redirectedfrom=MSDN
        }
    }

    //normal state, no button is pressed, trackball acts normally
    class StateNormal : State
    {
        public StateNormal()
            : base(new WinAPI.POINT())
        { }

        [Pure]
        public override Result Process(IntPtr wParam, WinAPI.MSLLHOOKSTRUCT llHookStruct, Properties.Settings settings)
        {
            if (WinAPI.MouseMessages.WM_XBUTTONDOWN == (WinAPI.MouseMessages)wParam)
            {
                var xbutton = GetXButton(llHookStruct.mouseData);
#if DEBUG
                System.Diagnostics.Debug.WriteLine("StateNormal -> xbutton " + xbutton + " down");
#endif
                //middle button pressed, go to StateMiddleDown
                if ((settings.useX1forMiddle && xbutton == 1) || (settings.useX2forMiddle && xbutton == 2))
                {
                    var input = InputMiddleDown(llHookStruct.pt);
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("StateNormal -> InputMiddleDown -> go to StateMiddleDown");
#endif
                    return new Result(new StateMiddleDown(llHookStruct.pt), CallNextHook.FALSE, input);
                }

                //scroll key pressed, go to StateScrollDown
                if ((settings.useX1forScroll && xbutton == 1) || (settings.useX2forScroll && xbutton == 2))
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("StateNormal -> go to StateScrollDown");
#endif
                    return new Result(new StateScrollDown(llHookStruct.pt), CallNextHook.FALSE, null);
                }
            }
            else if (WinAPI.MouseMessages.WM_XBUTTONUP == (WinAPI.MouseMessages)wParam)
            {
                var xbutton = GetXButton(llHookStruct.mouseData);
#if DEBUG
                System.Diagnostics.Debug.WriteLine("StateNormal -> xbutton " + xbutton + " up");
#endif
                if ((settings.useX1forScroll && xbutton == 1) || (settings.useX2forScroll && xbutton == 2))
                {
                    if (settings.scrollKeyAction == (int)ScrollKeyActions.KEY_TOGGLE)
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine("StateNormal -> KEY_TOGGLE -> StateNormal");
#endif
                        return new Result(new StateNormal(), CallNextHook.FALSE, null);
                    }
                }
            }

            //execute whatever key was pressed (including moving the mouse)
#if DEBUG
            if (WinAPI.MouseMessages.WM_MOUSEMOVE != (WinAPI.MouseMessages)wParam)
                System.Diagnostics.Debug.WriteLine("StateNormal -> execute keypress");
#endif            
            return new Result(this);
        }

        [Pure]
        public WinAPI.INPUT[] InputMiddleDown(WinAPI.POINT pt)
        {
            WinAPI.INPUT[] input = new WinAPI.INPUT[1];
            input[0].type = WinAPI.INPUT_MOUSE;
            input[0].mi.dx = pt.x;
            input[0].mi.dy = pt.y;
            input[0].mi.mouseData = 0x0;
            input[0].mi.dwFlags = (uint)WinAPI.MouseEvent.MOUSEEVENTF_MIDDLEDOWN; // middle button down
            input[0].mi.time = 0x0;
            input[0].mi.dwExtraInfo = IntPtr.Zero;
            return input;
        }
    }

    //scroll key is pressed, wait to scroll
    class StateScrollDown : State
    {
        public StateScrollDown(WinAPI.POINT origin)
            : base(origin)
        { }

        [Pure]
        public override Result Process(IntPtr wParam, WinAPI.MSLLHOOKSTRUCT llHookStruct, Properties.Settings settings)
        {
            if (WinAPI.MouseMessages.WM_XBUTTONUP == (WinAPI.MouseMessages)wParam)
            {
                var xbutton = GetXButton(llHookStruct.mouseData);
#if DEBUG
                System.Diagnostics.Debug.WriteLine("StateScrollDown -> xbutton " + xbutton + " up");
#endif
                if ((settings.useX1forScroll && xbutton == 1) || (settings.useX2forScroll && xbutton == 2))
                {
                    switch (Properties.Settings.Default.scrollKeyAction)
                    {
                        case (int)ScrollKeyActions.KEY_TOGGLE:
                            //if we use toggle key, do nothing and wait for either mouse move or key down
#if DEBUG
                            System.Diagnostics.Debug.WriteLine("StateScrollDown -> KEY_TOGGLE -> StateScrollDown");
#endif
                            return new Result(new StateScrollDown(llHookStruct.pt), CallNextHook.FALSE, null);

                        case (int)ScrollKeyActions.KEY_DEFAULT:
                            //simulate default X1=back, X2=forward behaviour
                            var inputdef = InputDefaultDown(llHookStruct.pt, xbutton);
#if DEBUG
                            System.Diagnostics.Debug.WriteLine("StateScrollDown -> InputDefaultDown -> StateDefaultKey");
#endif
                            return new Result(new StateDefaultDown(llHookStruct.pt), CallNextHook.FALSE, inputdef);

                        case (int)ScrollKeyActions.KEY_MIDDLE:
                            //simulate middle-click behaviour
                            var inputmid = InputMiddleClick(llHookStruct.pt);
#if DEBUG
                            System.Diagnostics.Debug.WriteLine("StateScrollDown -> InputMiddleClick -> StateNormal");
#endif
                            return new Result(new StateNormal(), CallNextHook.FALSE, inputmid);

                        case (int)ScrollKeyActions.KEY_NONE:
                        default:
                            //hold-to-scroll case, key-up means that we do not want to scroll anymore
#if DEBUG
                            System.Diagnostics.Debug.WriteLine("StateScrollDown -> StateNormal");
#endif
                            return new Result(new StateNormal(), CallNextHook.FALSE, null);
                    }
                }
            }
            else if (WinAPI.MouseMessages.WM_XBUTTONDOWN == (WinAPI.MouseMessages)wParam)
            {
                var xbutton = GetXButton(llHookStruct.mouseData);
#if DEBUG
                System.Diagnostics.Debug.WriteLine("StateScrollDown -> xbutton " + xbutton + " down");
#endif
                if ((settings.useX1forScroll && xbutton == 1) || (settings.useX2forScroll && xbutton == 2))
                {
                    if (settings.scrollKeyAction == (int)ScrollKeyActions.KEY_TOGGLE)
                    {
                        //exit scroll down mode if scroll key is pressed again
#if DEBUG
                        System.Diagnostics.Debug.WriteLine("StateScrollDown -> KEY_TOGGLE -> go to StateNormal");
#endif
                        return new Result(new StateNormal(), CallNextHook.FALSE, null);
                    }
                }
            }
            else if (WinAPI.MouseMessages.WM_MOUSEMOVE == (WinAPI.MouseMessages)wParam)
            {
                //scroll key is down, mouse is moved, go to scrolling state
#if DEBUG
                System.Diagnostics.Debug.WriteLine("StateScrollDown -> go to StateScroll");
#endif
                return new Result(new StateScroll(Origin, 0, 0), CallNextHook.FALSE, null);
            }

            //execute whatever key was pressed (including moving the mouse)
#if DEBUG
            if (WinAPI.MouseMessages.WM_MOUSEMOVE != (WinAPI.MouseMessages)wParam)
                System.Diagnostics.Debug.WriteLine("StateScrollDown -> execute keypress");
#endif    
            return new Result(this);
        }

        [Pure]
        public WinAPI.INPUT[] InputMiddleClick(WinAPI.POINT pt)
        {
            WinAPI.INPUT[] input = new WinAPI.INPUT[2];
            input[0].type = WinAPI.INPUT_MOUSE;
            input[0].mi.dx = pt.x;
            input[0].mi.dy = pt.y;
            input[0].mi.mouseData = 0x0;
            input[0].mi.dwFlags = (uint)WinAPI.MouseEvent.MOUSEEVENTF_MIDDLEDOWN; // middle button down
            input[0].mi.time = 0x0;
            input[0].mi.dwExtraInfo = IntPtr.Zero;
            input[1].type = WinAPI.INPUT_MOUSE;
            input[1].mi.dx = pt.x;
            input[1].mi.dy = pt.y;
            input[1].mi.mouseData = 0x0;
            input[1].mi.dwFlags = (uint)WinAPI.MouseEvent.MOUSEEVENTF_MIDDLEUP; // middle button up
            input[1].mi.time = 0x0;
            input[1].mi.dwExtraInfo = IntPtr.Zero;
            return input;
        }

        [Pure]
        public WinAPI.INPUT[] InputDefaultDown(WinAPI.POINT pt, uint xbutton)
        {
            WinAPI.INPUT[] input = new WinAPI.INPUT[1];
            input[0].type = WinAPI.INPUT_MOUSE;
            input[0].mi.dx = pt.x;
            input[0].mi.dy = pt.y;
            input[0].mi.mouseData = xbutton;
            input[0].mi.dwFlags = (uint)WinAPI.MouseEvent.MOUSEEVENTF_XDOWN; // default button down
            input[0].mi.time = 0x0;
            input[0].mi.dwExtraInfo = IntPtr.Zero;
            return input;
        }
    }

    //middle button is kept pressed
    class StateMiddleDown : State
    {
        public StateMiddleDown(WinAPI.POINT origin)
            : base(origin)
        { }

        [Pure]
        public override Result Process(IntPtr wParam, WinAPI.MSLLHOOKSTRUCT llHookStruct, Properties.Settings settings)
        {
            if (WinAPI.MouseMessages.WM_XBUTTONUP == (WinAPI.MouseMessages)wParam)
            {
                var xbutton = GetXButton(llHookStruct.mouseData);
#if DEBUG
                System.Diagnostics.Debug.WriteLine("StateMiddleDown -> xbutton " + xbutton + " up");
#endif
                //middle button released, go to normal state
                if ((settings.useX1forMiddle && xbutton == 1) || (settings.useX2forMiddle && xbutton == 2))
                {
                    var input = InputMiddleUp(llHookStruct.pt);
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("StateMiddleDown -> InputMiddleUp -> go to StateNormal");
#endif
                    return new Result(new StateNormal(), CallNextHook.FALSE, input);
                }
            }
            else if (WinAPI.MouseMessages.WM_XBUTTONDOWN == (WinAPI.MouseMessages)wParam)
            {
                //ignore all other xbutton presses here until middle button is released
#if DEBUG
                System.Diagnostics.Debug.WriteLine("StateMiddleDown -> StateMiddleDown");
#endif
                return new Result(new StateMiddleDown(llHookStruct.pt), CallNextHook.FALSE, null);
            }

            //execute whatever key was pressed (including moving the mouse)
#if DEBUG
            if (WinAPI.MouseMessages.WM_MOUSEMOVE != (WinAPI.MouseMessages)wParam)
                System.Diagnostics.Debug.WriteLine("StateMiddleDown -> execute keypress");
#endif  
            return new Result(this);
        }

        [Pure]
        public WinAPI.INPUT[] InputMiddleUp(WinAPI.POINT pt)
        {
            WinAPI.INPUT[] input = new WinAPI.INPUT[1];
            input[0].type = WinAPI.INPUT_MOUSE;
            input[0].mi.dx = pt.x;
            input[0].mi.dy = pt.y;
            input[0].mi.mouseData = 0x0;
            input[0].mi.dwFlags = (uint)WinAPI.MouseEvent.MOUSEEVENTF_MIDDLEUP; // middle button up
            input[0].mi.time = 0x0;
            input[0].mi.dwExtraInfo = IntPtr.Zero;
            return input;
        }
    }

    //default xbutton action
    class StateDefaultDown : State
    {
        public StateDefaultDown(WinAPI.POINT origin)
            : base(origin)
        { }

        [Pure]
        public override Result Process(IntPtr wParam, WinAPI.MSLLHOOKSTRUCT llHookStruct, Properties.Settings settings)
        {
            if (WinAPI.MouseMessages.WM_XBUTTONDOWN == (WinAPI.MouseMessages)wParam)
            {
                //send xbutton up event to normal state
                var xbutton = GetXButton(llHookStruct.mouseData);
                var input = InputDefaultUp(llHookStruct.pt, xbutton);
#if DEBUG
                System.Diagnostics.Debug.WriteLine("StateDefaultDown -> InputDefaultUp -> StateNormal");
#endif
                return new Result(new StateNormal(), CallNextHook.FALSE, input);
            }

            //execute whatever key was pressed (including moving the mouse)
#if DEBUG
            if (WinAPI.MouseMessages.WM_MOUSEMOVE != (WinAPI.MouseMessages)wParam)
                System.Diagnostics.Debug.WriteLine("StateDefaultDown -> execute keypress");
#endif
            return new Result(this);
        }

        [Pure]
        public WinAPI.INPUT[] InputDefaultUp(WinAPI.POINT pt, uint xbutton)
        {
            WinAPI.INPUT[] input = new WinAPI.INPUT[1];
            input[0].type = WinAPI.INPUT_MOUSE;
            input[0].mi.dx = pt.x;
            input[0].mi.dy = pt.y;
            input[0].mi.mouseData = xbutton;
            input[0].mi.dwFlags = (uint)WinAPI.MouseEvent.MOUSEEVENTF_XUP; // default button up
            input[0].mi.time = 0x0;
            input[0].mi.dwExtraInfo = IntPtr.Zero;
            return input;
        }
    }

    //treat continuous scrolling
    class StateScroll : State
    {
        //private static readonly int X_THRESHOLD = 20;   // threshold in pixels to trigger wheel event
        //private static readonly int Y_THRESHOLD = 20;   // threshold in pixels to trigger wheel event
        private static readonly uint WHEEL_FACTOR = 1; // number of wheel events. The lines scrolled per wheel event are determined by the Microsoft Windows mouse wheel settings.

        public int Xcount { get; } // accumulated horizontal movement while in state SCROLL
        public int Ycount { get; } // accumulated vertical   movement while in state SCROLL

        public StateScroll(WinAPI.POINT origin, int xcount, int ycount)
            : base(origin)
        {
            Xcount = xcount;
            Ycount = ycount;
        }

        [Pure]
        public override Result Process(IntPtr wParam, WinAPI.MSLLHOOKSTRUCT llHookStruct, Properties.Settings settings)
        {
            if (WinAPI.MouseMessages.WM_XBUTTONUP == (WinAPI.MouseMessages)wParam)
            {
                var xbutton = GetXButton(llHookStruct.mouseData);
#if DEBUG
                System.Diagnostics.Debug.WriteLine("StateScroll -> xbutton " + xbutton + " up");
#endif
                if ((settings.useX1forScroll && xbutton == 1) || (settings.useX2forScroll && xbutton == 2))
                {
                    if (settings.scrollKeyAction == (int)ScrollKeyActions.KEY_TOGGLE)
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine("StateScroll -> KEY_TOGGLE -> StateScroll");
#endif
                        return new Result(new StateScroll(Origin, 0, 0), CallNextHook.FALSE, null);
                    }
                    else
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine("StateScroll -> !KEY_TOGGLE -> go to StateNormal");
#endif
                        return new Result(new StateNormal(), CallNextHook.FALSE, null);
                    }
                }
            }
            else if (WinAPI.MouseMessages.WM_XBUTTONDOWN == (WinAPI.MouseMessages)wParam)
            {
                var xbutton = GetXButton(llHookStruct.mouseData);
#if DEBUG
                System.Diagnostics.Debug.WriteLine("StateScroll -> xbutton " + xbutton + " down");
#endif
                if ((settings.useX1forScroll && xbutton == 1) || (settings.useX2forScroll && xbutton == 2))
                {
                    if (settings.scrollKeyAction == (int)ScrollKeyActions.KEY_TOGGLE)
                    {
                        //exit scroll state if we toggle scroll key
#if DEBUG
                        System.Diagnostics.Debug.WriteLine("StateScroll -> KEY_TOGGLE -> StateNormal");
#endif
                        return new Result(new StateNormal(), CallNextHook.FALSE, null);
                    }
                }
            }

            WinAPI.INPUT[] input = null;

            //simulate scrolling by mouse move
            if (WinAPI.MouseMessages.WM_MOUSEMOVE == (WinAPI.MouseMessages)wParam)
            {
                int x = Xcount;
                int y = Ycount;

                if (Xcount < -settings.scrollSpeed || Xcount > settings.scrollSpeed)
                {
                    uint mouseData = (uint)((settings.reverseHorizontalScroll ? -1 : 1) * WinAPI.WHEEL_DELTA * Xcount / settings.scrollSpeed);
                    x = Xcount - (Xcount / settings.scrollSpeed) * settings.scrollSpeed; // integer division
                    if (settings.preferAxis)
                    {
                        y = 0;
                    }
                    input = InputWheel(llHookStruct.pt, mouseData, WinAPI.MouseEvent.MOUSEEVENTF_HWHEEL);
                }

                if (Ycount < -settings.scrollSpeed || Ycount > settings.scrollSpeed)
                {
                    uint mouseData = (uint)((settings.reverseVerticalScroll ? 1 : -1) * WinAPI.WHEEL_DELTA * Ycount / settings.scrollSpeed);
                    if (settings.preferAxis)
                    {
                        x = 0;
                    }
                    y = Ycount - (Ycount / settings.scrollSpeed) * settings.scrollSpeed; // integer division
                    input = InputWheel(llHookStruct.pt, mouseData, WinAPI.MouseEvent.MOUSEEVENTF_WHEEL);
                }

                x += llHookStruct.pt.x - Origin.x;
                y += llHookStruct.pt.y - Origin.y;

                return new Result(new StateScroll(Origin, x, y), CallNextHook.FALSE, input);
            }

            //execute whatever key was pressed (including moving the mouse)
#if DEBUG
            if (WinAPI.MouseMessages.WM_MOUSEMOVE != (WinAPI.MouseMessages)wParam)
                System.Diagnostics.Debug.WriteLine("StateScroll -> execute keypress");
#endif  
            return new Result(this);
        }

        // MOUSEEVENTF_HWHEEL or MOUSEEVENTF_WHEEL
        [Pure]
        private WinAPI.INPUT[] InputWheel(WinAPI.POINT pt, uint mouseData, WinAPI.MouseEvent wheel_type)
        {
            var input = new WinAPI.INPUT[WHEEL_FACTOR];
            for (uint i = 0; i < WHEEL_FACTOR; ++i)
            {
                input[i].type = WinAPI.INPUT_MOUSE;
                input[i].mi.dx = pt.x;
                input[i].mi.dy = pt.y;
                input[i].mi.mouseData = mouseData;
                input[i].mi.dwFlags = (uint)wheel_type;
                input[i].mi.time = 0x0;
                input[i].mi.dwExtraInfo = IntPtr.Zero;
            }
            return input;
        }
    }
}
