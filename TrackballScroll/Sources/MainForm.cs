using System;
using System.Collections.Concurrent;
using System.Windows.Forms;

/*
 * Systray application framework for TrackballScroll.
 * Contains the Main method and handling of the systray menu.
 *
 * @author: Martin Seelge
 *
 * Credits:
 * Inspired by
 * https://alanbondo.wordpress.com/2008/06/22/creating-a-system-tray-app-with-c/
 */
namespace TrackballScroll
{
    public enum ScrollKeyActions
    {
        KEY_NONE,
        KEY_TOGGLE,
        KEY_DEFAULT,
        KEY_MIDDLE
    }
    
    public enum ScrollSpeeds
    {
        SPD_LOW = 30,
        SPD_MEDIUM = 20,
        SPD_HIGH = 10
    }

    public class MainForm : Form
    {
        [STAThread]
        public static void Main()
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("Running TrackballScroll in debug mode"); // Writes to VS output view
#endif

            Application.Run(new MainForm());
        }

        private NotifyIcon trayIcon;
        private MenuItem itemEnabled;
        private MenuItem itemUseX1forScroll;
        private MenuItem itemUseX1forMiddle;
        private MenuItem itemUseX1forDefault;
        private MenuItem itemUseX2forScroll;
        private MenuItem itemUseX2forMiddle;
        private MenuItem itemUseX2forDefault;
        private MenuItem itemPreferAxis;
        private MenuItem itemReverseVerticalScroll;
        private MenuItem itemReverseHorizontalScroll;
        private MenuItem itemScrollSpeedLow;
        private MenuItem itemScrollSpeedMedium;
        private MenuItem itemScrollSpeedHigh;
        private MenuItem itemScrollKeyNone;
        private MenuItem itemScrollKeyToggle;
        private MenuItem itemScrollKeyDefault;
        private MenuItem itemScrollKeyMiddle;


        private MouseHookTrackballScroll mouseHook;
        private MouseEventDispatcher mouseEventDispatcher;

        public MainForm()
        {
            Properties.Settings.Default.Upgrade();

            var queue = new ConcurrentQueue<MouseEvent>();
            mouseHook = new MouseHookTrackballScroll(queue);
            mouseEventDispatcher = new MouseEventDispatcher(queue);

            itemEnabled = new MenuItem(Properties.Resources.TextButtonHookEnabled, OnToggleHook);
            itemEnabled.Checked = true;
 
            itemUseX1forScroll = new MenuItem(Properties.Resources.TextButtonHookUseforScroll, OnToggleUseX1forScroll);
            itemUseX1forScroll.Checked = Properties.Settings.Default.useX1forScroll;

            itemUseX1forMiddle = new MenuItem(Properties.Resources.TextButtonHookUseforMiddle, OnToggleUseX1forMiddle);
            itemUseX1forMiddle.Checked = Properties.Settings.Default.useX1forMiddle;

            itemUseX1forDefault = new MenuItem(Properties.Resources.TextButtonHookUseforDefault, OnToggleUseX1forDefault);
            itemUseX1forDefault.Checked = !itemUseX1forScroll.Checked && !itemUseX1forMiddle.Checked;

            itemUseX2forScroll = new MenuItem(Properties.Resources.TextButtonHookUseforScroll, OnToggleUseX2forScroll);
            itemUseX2forScroll.Checked = Properties.Settings.Default.useX2forScroll;

            itemUseX2forMiddle = new MenuItem(Properties.Resources.TextButtonHookUseforMiddle, OnToggleUseX2forMiddle);
            itemUseX2forMiddle.Checked = Properties.Settings.Default.useX2forMiddle;

            itemUseX2forDefault = new MenuItem(Properties.Resources.TextButtonHookUseforDefault, OnToggleUseX2forDefault);
            itemUseX2forDefault.Checked = !itemUseX2forScroll.Checked && !itemUseX2forMiddle.Checked;

            itemScrollSpeedLow = new MenuItem(Properties.Resources.TextButtonSpeedLow, OnToggleScrollSpeedLow);
            itemScrollSpeedMedium = new MenuItem(Properties.Resources.TextButtonSpeedMedium, OnToggleScrollSpeedMedium);
            itemScrollSpeedHigh = new MenuItem(Properties.Resources.TextButtonSpeedHigh, OnToggleScrollSpeedHigh);
            switch(Properties.Settings.Default.scrollSpeed)
            {
                case (int)ScrollSpeeds.SPD_HIGH:
                    itemScrollSpeedHigh.Checked = true;
                    break;
                case (int)ScrollSpeeds.SPD_MEDIUM:
                    itemScrollSpeedMedium.Checked = true;
                    break;
                case (int)ScrollSpeeds.SPD_LOW: 
                default:
                    itemScrollSpeedLow.Checked = true;
                    break;
            }

            itemScrollKeyNone = new MenuItem(Properties.Resources.TextButtonActionNone, OnToggleScrollKeyNone);
            itemScrollKeyToggle = new MenuItem(Properties.Resources.TextButtonActionToggle, OnToggleScrollKeyToggle);
            itemScrollKeyDefault = new MenuItem(Properties.Resources.TextButtonActionDefault, OnToggleScrollKeyDefault);
            itemScrollKeyMiddle = new MenuItem(Properties.Resources.TextButtonActionMiddle, OnToggleScrollKeyMiddle);
            switch (Properties.Settings.Default.scrollKeyAction)
            {
                case (int)ScrollKeyActions.KEY_TOGGLE:
                    itemScrollKeyToggle.Checked = true;
                    break;
                case (int)ScrollKeyActions.KEY_DEFAULT:
                    itemScrollKeyDefault.Checked = true;
                    break;
                case (int)ScrollKeyActions.KEY_MIDDLE:
                    itemScrollKeyMiddle.Checked = true;
                    break;
                case (int)ScrollKeyActions.KEY_NONE:
                default:
                    itemScrollKeyNone.Checked = true;
                    break;
            }

            itemPreferAxis = new MenuItem(Properties.Resources.TextButtonPreferAxis, OnToggleAxis);
            itemPreferAxis.Checked = Properties.Settings.Default.preferAxis;

            itemReverseVerticalScroll = new MenuItem(Properties.Resources.TextButtonReverseVerticalScroll, OnToggleReverseVerticalScroll);
            itemReverseVerticalScroll.Checked = Properties.Settings.Default.reverseVerticalScroll;

            itemReverseHorizontalScroll = new MenuItem(Properties.Resources.TextButtonReverseHorizontalScroll, OnToggleReverseHorizontalScroll);
            itemReverseHorizontalScroll.Checked = Properties.Settings.Default.reverseHorizontalScroll;

            ContextMenu trayMenu = new ContextMenu();
            trayMenu.MenuItems.Add(itemEnabled);

            trayMenu.MenuItems.Add("-");

            MenuItem itemX1Action = new MenuItem(Properties.Resources.TextButtonX1Action);
            itemX1Action.MenuItems.Add(itemUseX1forScroll);
            itemX1Action.MenuItems.Add(itemUseX1forMiddle);
            itemX1Action.MenuItems.Add(itemUseX1forDefault);
            trayMenu.MenuItems.Add(itemX1Action);

            MenuItem itemX2Action = new MenuItem(Properties.Resources.TextButtonX2Action);
            itemX2Action.MenuItems.Add(itemUseX2forScroll);
            itemX2Action.MenuItems.Add(itemUseX2forMiddle);
            itemX2Action.MenuItems.Add(itemUseX2forDefault);
            trayMenu.MenuItems.Add(itemX2Action);

            trayMenu.MenuItems.Add("-");

            MenuItem itemScrollSpeed = new MenuItem(Properties.Resources.TextButtonScrollSpeed);
            itemScrollSpeed.MenuItems.Add(itemScrollSpeedLow);
            itemScrollSpeed.MenuItems.Add(itemScrollSpeedMedium);
            itemScrollSpeed.MenuItems.Add(itemScrollSpeedHigh);
            trayMenu.MenuItems.Add(itemScrollSpeed);

            MenuItem itemScrollAction = new MenuItem(Properties.Resources.TextButtonScrollAction);
            itemScrollAction.MenuItems.Add(itemScrollKeyNone);
            itemScrollAction.MenuItems.Add(itemScrollKeyToggle);
            itemScrollAction.MenuItems.Add(itemScrollKeyDefault);
            itemScrollAction.MenuItems.Add(itemScrollKeyMiddle);
            trayMenu.MenuItems.Add(itemScrollAction);

            trayMenu.MenuItems.Add(itemPreferAxis);
            trayMenu.MenuItems.Add(itemReverseVerticalScroll);
            trayMenu.MenuItems.Add(itemReverseHorizontalScroll);

            trayMenu.MenuItems.Add("-");

            trayMenu.MenuItems.Add(Properties.Resources.TextButtonAbout, OnAbout);
            trayMenu.MenuItems.Add(Properties.Resources.TextButtonExit, OnExit);
            

            trayIcon = new NotifyIcon
            {
                Text = Properties.Resources.TextTitle,
                Icon = Properties.Resources.icon,
                ContextMenu = trayMenu,
                Visible = true
            };
        }
        
        protected override void OnLoad(EventArgs e)
        {
            Visible = false;       // Hide form window.
            ShowInTaskbar = false; // Remove from taskbar.
            base.OnLoad(e);
        }

        private void OnToggleHook(object sender, EventArgs e)
        {
            if(itemEnabled.Checked)
            {
                mouseHook.Unhook();
                itemEnabled.Checked = false;
                itemUseX1forScroll.Enabled = false;
                itemUseX1forMiddle.Enabled = false;
                itemUseX2forScroll.Enabled = false;
                itemUseX2forMiddle.Enabled = false;
                itemPreferAxis.Enabled = false;
                itemReverseVerticalScroll.Enabled = false;
                itemReverseHorizontalScroll.Enabled = false;
            }
            else
            {
                mouseHook.Hook();
                itemEnabled.Checked = true;
                itemUseX1forScroll.Enabled = true;
                itemUseX1forMiddle.Enabled = true;
                itemUseX2forScroll.Enabled = true;
                itemUseX2forMiddle.Enabled = true;
                itemPreferAxis.Enabled = true;
                itemReverseVerticalScroll.Enabled = true;
                itemReverseHorizontalScroll.Enabled = true;
            }
        }

        private void OnToggleUseX1forScroll(object sender, EventArgs e)
        {
            itemUseX1forScroll.Checked = true;
            itemUseX1forMiddle.Checked = false;
            itemUseX1forDefault.Checked = false;
            Properties.Settings.Default.useX1forScroll = true;
            Properties.Settings.Default.useX1forMiddle = false;
            Properties.Settings.Default.Save();
        }

        private void OnToggleUseX1forMiddle(object sender, EventArgs e)
        {
            itemUseX1forScroll.Checked = false;
            itemUseX1forMiddle.Checked = true;
            itemUseX1forDefault.Checked = false;
            Properties.Settings.Default.useX1forScroll = false;
            Properties.Settings.Default.useX1forMiddle = true;
            Properties.Settings.Default.Save();
        }

        private void OnToggleUseX1forDefault(object sender, EventArgs e)
        {
            itemUseX1forScroll.Checked = false;
            itemUseX1forMiddle.Checked = false;
            itemUseX1forDefault.Checked = true;
            Properties.Settings.Default.useX1forScroll = false;
            Properties.Settings.Default.useX1forMiddle = false;
            Properties.Settings.Default.Save();
        }

        private void OnToggleUseX2forScroll(object sender, EventArgs e)
        {
            itemUseX2forScroll.Checked = true;
            itemUseX2forMiddle.Checked = false;
            itemUseX2forDefault.Checked = false;
            Properties.Settings.Default.useX2forScroll = true;
            Properties.Settings.Default.useX2forMiddle = false;
            Properties.Settings.Default.Save();
        }

        private void OnToggleUseX2forMiddle(object sender, EventArgs e)
        {
            itemUseX2forScroll.Checked = false;
            itemUseX2forMiddle.Checked = true;
            itemUseX2forDefault.Checked = false;
            Properties.Settings.Default.useX2forScroll = false;
            Properties.Settings.Default.useX2forMiddle = true;
            Properties.Settings.Default.Save();
        }

        private void OnToggleUseX2forDefault(object sender, EventArgs e)
        {
            itemUseX2forScroll.Checked = false;
            itemUseX2forMiddle.Checked = false;
            itemUseX2forDefault.Checked = true;
            Properties.Settings.Default.useX2forScroll = false;
            Properties.Settings.Default.useX2forMiddle = false;
            Properties.Settings.Default.Save();
        }

        private void OnToggleScrollSpeedLow(object sender, EventArgs e)
        {
            itemScrollSpeedLow.Checked = true;
            itemScrollSpeedMedium.Checked = false;
            itemScrollSpeedHigh.Checked = false;
            Properties.Settings.Default.scrollSpeed = (int)ScrollSpeeds.SPD_LOW;
            Properties.Settings.Default.Save();
        }

        private void OnToggleScrollSpeedMedium(object sender, EventArgs e)
        {
            itemScrollSpeedLow.Checked = false;
            itemScrollSpeedMedium.Checked = true;
            itemScrollSpeedHigh.Checked = false;
            Properties.Settings.Default.scrollSpeed = (int)ScrollSpeeds.SPD_MEDIUM;
            Properties.Settings.Default.Save();
        }

        private void OnToggleScrollSpeedHigh(object sender, EventArgs e)
        {
            itemScrollSpeedLow.Checked = false;
            itemScrollSpeedMedium.Checked = false;
            itemScrollSpeedHigh.Checked = true;
            Properties.Settings.Default.scrollSpeed = (int)ScrollSpeeds.SPD_HIGH;
            Properties.Settings.Default.Save();
        }

        private void OnToggleScrollKeyNone(object sender, EventArgs e)
        {
            itemScrollKeyNone.Checked = true;
            itemScrollKeyToggle.Checked = false;
            itemScrollKeyDefault.Checked = false;
            itemScrollKeyMiddle.Checked = false;
            Properties.Settings.Default.scrollKeyAction = (int)ScrollKeyActions.KEY_NONE;
            Properties.Settings.Default.Save();
        }

        private void OnToggleScrollKeyToggle(object sender, EventArgs e)
        {
            itemScrollKeyNone.Checked = false;
            itemScrollKeyToggle.Checked = true;
            itemScrollKeyDefault.Checked = false;
            itemScrollKeyMiddle.Checked = false;
            Properties.Settings.Default.scrollKeyAction = (int)ScrollKeyActions.KEY_TOGGLE;
            Properties.Settings.Default.Save();
        }

        private void OnToggleScrollKeyDefault(object sender, EventArgs e)
        {
            itemScrollKeyNone.Checked = false;
            itemScrollKeyToggle.Checked = false;
            itemScrollKeyDefault.Checked = true;
            itemScrollKeyMiddle.Checked = false;
            Properties.Settings.Default.scrollKeyAction = (int)ScrollKeyActions.KEY_DEFAULT;
            Properties.Settings.Default.Save();
        }

        private void OnToggleScrollKeyMiddle(object sender, EventArgs e)
        {
            itemScrollKeyNone.Checked = false;
            itemScrollKeyToggle.Checked = false;
            itemScrollKeyDefault.Checked = false;
            itemScrollKeyMiddle.Checked = true;
            Properties.Settings.Default.scrollKeyAction = (int)ScrollKeyActions.KEY_MIDDLE;
            Properties.Settings.Default.Save();
        }

        private void OnToggleAxis(object sender, EventArgs e)
        {
            itemPreferAxis.Checked = !itemPreferAxis.Checked;
            Properties.Settings.Default.preferAxis = itemPreferAxis.Checked;
            Properties.Settings.Default.Save();
        }

        private void OnToggleReverseVerticalScroll(object sender, EventArgs e)
        {
            itemReverseVerticalScroll.Checked = !itemReverseVerticalScroll.Checked;
            Properties.Settings.Default.reverseVerticalScroll = itemReverseVerticalScroll.Checked;
            Properties.Settings.Default.Save();
        }

        private void OnToggleReverseHorizontalScroll(object sender, EventArgs e)
        {
            itemReverseHorizontalScroll.Checked = !itemReverseHorizontalScroll.Checked;
            Properties.Settings.Default.reverseHorizontalScroll = itemReverseHorizontalScroll.Checked;
            Properties.Settings.Default.Save();
        }

        private void OnAbout(object sender, EventArgs e)
        {
            Form about = new AboutBox();
            about.ShowDialog();
        }

        private void OnExit(object sender, EventArgs e)
        {
            mouseHook.Unhook();
            Application.Exit();
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                mouseEventDispatcher.Dispose();
                trayIcon.Dispose();
            }

            base.Dispose(isDisposing);
        }
    }
}
