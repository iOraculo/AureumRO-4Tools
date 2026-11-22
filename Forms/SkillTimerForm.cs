using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Input;
using _4RTools.Model;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public partial class SkillTimerForm : Form, IObserver
    {
        public const int TOTAL_TIMER_SLOTS = 6;

        private const int CARD_WIDTH = 174;
        private const int CARD_HEIGHT = 122;
        private const int COLS_PER_ROW = 3;
        private const int GRID_START_X = 12;
        private const int GRID_START_Y = 118;
        private const int COL_STEP_X = 184;
        private const int ROW_STEP_Y = 132;

        private sealed class TimerSlotUi
        {
            public int SlotNumber;
            public GroupBox Group;
            public TextBox KeyBox;
            public NumericUpDown DelayBox;
            public Button BtnClear;
            public Button BtnRemove;
            public bool IsVisible;
        }

        private readonly TimerSlotUi[] slots = new TimerSlotUi[TOTAL_TIMER_SLOTS];
        private GroupBox gbAntiPositionBug;
        private CheckBox chkRefreshEnabled;
        private Label lblRefreshCmd;
        private TextBox txtRefreshCmd;
        private CheckBox chkRefreshPauseSit;
        private Label lblRefreshHotkey;
        private TextBox txtRefreshHotkey;
        private Button btnClearRefreshHotkey;
        private CheckBox chkRefreshAutoInterval;
        private NumericUpDown numRefreshInterval;
        private Label lblRefreshIntervalSec;
        private Button btnTestRefresh;
        private Label lblRefreshHint;
        private Button btnAddTimer;
        private ToolTip timerToolTip;
        private Font labelFont;
        private Font inputFont;
        private Font hintFont;
        private bool isUpdatingUi;

        public SkillTimerForm(Subject subject)
        {
            InitializeComponent();
            BuildSimplifiedUi();
            subject.Attach(this);
            SyncFromProfile();
        }

        private void BuildSimplifiedUi()
        {
            this.SuspendLayout();
            try
            {
                this.Controls.Clear();
                this.AutoScroll = true;
                this.timerToolTip = FormUtils.CreateHelpToolTip(this.components);
                this.labelFont = new Font("Microsoft Sans Serif", 8.5F);
                this.inputFont = new Font("Microsoft Sans Serif", 10F);
                this.hintFont = new Font("Segoe UI", 7.75F, FontStyle.Regular);

                BuildAntiPositionBugCard();

                for (int i = 0; i < TOTAL_TIMER_SLOTS; i++)
                {
                    int slotNum = i + 1;
                    TimerSlotUi slot = CreateTimerSlotUi(slotNum);
                    slot.IsVisible = (slotNum == 1);
                    this.slots[i] = slot;
                    this.Controls.Add(slot.Group);
                }

                this.btnAddTimer = new Button
                {
                    Cursor = System.Windows.Forms.Cursors.Hand,
                    Name = "btnAddSkillTimer",
                    Size = new Size(156, 25),
                    Text = "+ Add Timer",
                    UseVisualStyleBackColor = true
                };
                this.btnAddTimer.Click += (s, e) => AddNextTimer();
                this.Controls.Add(this.btnAddTimer);

                RefreshTooltips();
                LayoutVisibleTimers();
            }
            finally
            {
                this.ResumeLayout(true);
            }
        }

        private void BuildAntiPositionBugCard()
        {
            this.gbAntiPositionBug = new GroupBox
            {
                Location = new Point(12, 8),
                Name = "gbAntiPositionBug",
                Size = new Size(544, 102),
                TabStop = false,
                Text = "Anti-Position Bug (@refresh)"
            };

            this.chkRefreshEnabled = new CheckBox
            {
                AutoSize = true,
                Cursor = System.Windows.Forms.Cursors.Hand,
                Font = this.labelFont,
                Location = new Point(12, 20),
                Name = "chkRefreshEnabled",
                Text = "Enable @refresh",
                UseVisualStyleBackColor = true
            };
            this.chkRefreshEnabled.CheckedChanged += OnRefreshSettingsChanged;

            this.lblRefreshCmd = new Label
            {
                AutoSize = true,
                Font = this.labelFont,
                Location = new Point(146, 21),
                Name = "lblRefreshCmd",
                Text = "Command:"
            };

            this.txtRefreshCmd = new TextBox
            {
                Font = this.labelFont,
                Location = new Point(214, 18),
                MaxLength = 40,
                Name = "txtRefreshCmd",
                Size = new Size(106, 20),
                Tag = FormUtils.SKIP_RESET_TAG,
                Text = AutoRefreshCommand.DEFAULT_COMMAND
            };
            this.txtRefreshCmd.Leave += OnRefreshCommandLeave;
            this.txtRefreshCmd.TextChanged += OnRefreshSettingsChanged;

            this.chkRefreshPauseSit = new CheckBox
            {
                AutoSize = true,
                Checked = true,
                Cursor = System.Windows.Forms.Cursors.Hand,
                Font = this.labelFont,
                Location = new Point(336, 20),
                Name = "chkRefreshPauseSit",
                Text = "Pause when sitting",
                UseVisualStyleBackColor = true
            };
            this.chkRefreshPauseSit.CheckedChanged += OnRefreshSettingsChanged;

            this.lblRefreshHotkey = new Label
            {
                AutoSize = true,
                Font = this.labelFont,
                Location = new Point(12, 50),
                Name = "lblRefreshHotkey",
                Text = "Hotkey (1-tap):"
            };

            this.txtRefreshHotkey = new TextBox
            {
                Font = this.labelFont,
                Location = new Point(114, 47),
                Name = "txtRefreshHotkey",
                Size = new Size(58, 20),
                Text = Key.None.ToString()
            };
            this.txtRefreshHotkey.KeyDown += new System.Windows.Forms.KeyEventHandler(FormUtils.OnKeyDown);
            this.txtRefreshHotkey.KeyPress += new KeyPressEventHandler(FormUtils.OnKeyPress);
            this.txtRefreshHotkey.TextChanged += OnRefreshHotkeyChanged;

            this.btnClearRefreshHotkey = new Button
            {
                Cursor = System.Windows.Forms.Cursors.Hand,
                Location = new Point(176, 46),
                Name = "btnClearRefreshHotkey",
                Size = new Size(52, 22),
                Text = "Clear",
                UseVisualStyleBackColor = true
            };
            this.btnClearRefreshHotkey.Click += (s, e) =>
            {
                this.txtRefreshHotkey.Text = Key.None.ToString();
            };

            this.chkRefreshAutoInterval = new CheckBox
            {
                AutoSize = true,
                Cursor = System.Windows.Forms.Cursors.Hand,
                Font = this.labelFont,
                Location = new Point(246, 50),
                Name = "chkRefreshAutoInterval",
                Text = "Auto every:",
                UseVisualStyleBackColor = true
            };
            this.chkRefreshAutoInterval.CheckedChanged += OnRefreshSettingsChanged;

            this.numRefreshInterval = new NumericUpDown
            {
                Font = this.labelFont,
                Location = new Point(344, 47),
                Maximum = AutoRefreshCommand.MAX_INTERVAL_SECONDS,
                Minimum = AutoRefreshCommand.MIN_INTERVAL_SECONDS,
                Name = "numRefreshInterval",
                Size = new Size(58, 20),
                Tag = FormUtils.SKIP_RESET_TAG,
                Value = AutoRefreshCommand.DEFAULT_INTERVAL_SECONDS
            };
            this.numRefreshInterval.ValueChanged += OnRefreshSettingsChanged;

            this.lblRefreshIntervalSec = new Label
            {
                AutoSize = true,
                Font = this.labelFont,
                Location = new Point(406, 50),
                Name = "lblRefreshIntervalSec",
                Text = "sec"
            };

            this.btnTestRefresh = new Button
            {
                Cursor = System.Windows.Forms.Cursors.Hand,
                Location = new Point(464, 45),
                Name = "btnTestRefresh",
                Size = new Size(68, 24),
                Text = "Test",
                UseVisualStyleBackColor = true
            };
            this.btnTestRefresh.Click += OnTestRefreshClick;

            this.lblRefreshHint = new Label
            {
                AutoSize = false,
                Font = this.hintFont,
                Location = new Point(12, 77),
                Name = "lblRefreshHint",
                Size = new Size(520, 18),
                Tag = ThemeManager.TAG_MUTED_TEXT,
                TextAlign = ContentAlignment.MiddleLeft
            };

            this.gbAntiPositionBug.Controls.Add(this.chkRefreshEnabled);
            this.gbAntiPositionBug.Controls.Add(this.lblRefreshCmd);
            this.gbAntiPositionBug.Controls.Add(this.txtRefreshCmd);
            this.gbAntiPositionBug.Controls.Add(this.chkRefreshPauseSit);
            this.gbAntiPositionBug.Controls.Add(this.lblRefreshHotkey);
            this.gbAntiPositionBug.Controls.Add(this.txtRefreshHotkey);
            this.gbAntiPositionBug.Controls.Add(this.btnClearRefreshHotkey);
            this.gbAntiPositionBug.Controls.Add(this.chkRefreshAutoInterval);
            this.gbAntiPositionBug.Controls.Add(this.numRefreshInterval);
            this.gbAntiPositionBug.Controls.Add(this.lblRefreshIntervalSec);
            this.gbAntiPositionBug.Controls.Add(this.btnTestRefresh);
            this.gbAntiPositionBug.Controls.Add(this.lblRefreshHint);

            this.Controls.Add(this.gbAntiPositionBug);
        }

        private void OnRefreshSettingsChanged(object sender, EventArgs e)
        {
            if (this.numRefreshInterval != null && this.chkRefreshAutoInterval != null)
            {
                this.numRefreshInterval.Enabled = this.chkRefreshAutoInterval.Checked;
            }

            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            Profile current = ProfileSingleton.GetCurrent();
            if (current == null || current.AutoRefreshCommand == null)
            {
                return;
            }

            current.AutoRefreshCommand.Enabled = this.chkRefreshEnabled.Checked;
            current.AutoRefreshCommand.AutoIntervalEnabled = this.chkRefreshAutoInterval.Checked;
            current.AutoRefreshCommand.IntervalSeconds = decimal.ToInt32(this.numRefreshInterval.Value);
            current.AutoRefreshCommand.PauseWhenSitting = this.chkRefreshPauseSit.Checked;
            current.AutoRefreshCommand.CommandText = AutoRefreshCommand.NormalizeCommand(this.txtRefreshCmd.Text);
            ProfileSingleton.SetConfiguration(current.AutoRefreshCommand);
        }

        private void OnRefreshCommandLeave(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || this.txtRefreshCmd == null)
            {
                return;
            }

            string normalized = AutoRefreshCommand.NormalizeCommand(this.txtRefreshCmd.Text);
            if (!string.Equals(this.txtRefreshCmd.Text, normalized, StringComparison.Ordinal))
            {
                this.isUpdatingUi = true;
                try
                {
                    this.txtRefreshCmd.Text = normalized;
                }
                finally
                {
                    this.isUpdatingUi = false;
                }
            }
        }

        private void OnRefreshHotkeyChanged(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            Profile current = ProfileSingleton.GetCurrent();
            if (current == null || current.AutoRefreshCommand == null)
            {
                return;
            }

            Key key;
            if (KeyHelper.TryParseKey(this.txtRefreshHotkey.Text, out key))
            {
                current.AutoRefreshCommand.Hotkey = key;
                ProfileSingleton.SetConfiguration(current.AutoRefreshCommand);
            }
        }

        private void OnTestRefreshClick(object sender, EventArgs e)
        {
            Client roClient = ClientSingleton.GetClient();
            if (roClient == null || !roClient.IsProcessAlive())
            {
                MessageBox.Show(
                    LocalizationManager.T("msg.select_client_first"),
                    LocalizationManager.T("title.warning"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            Profile current = ProfileSingleton.GetCurrent();
            AutoRefreshCommand cmd = current != null && current.AutoRefreshCommand != null
                ? current.AutoRefreshCommand
                : new AutoRefreshCommand();
            cmd.CommandText = AutoRefreshCommand.NormalizeCommand(this.txtRefreshCmd != null ? this.txtRefreshCmd.Text : AutoRefreshCommand.DEFAULT_COMMAND);
            cmd.SendRefreshCommand(roClient);
        }

        private void RefreshTooltips()
        {
            if (this.lblRefreshHint != null)
            {
                this.lblRefreshHint.Text = LocalizationManager.T("refresh.hint");
            }

            if (this.timerToolTip == null)
            {
                return;
            }

            if (this.chkRefreshEnabled != null)
            {
                this.timerToolTip.SetToolTip(this.chkRefreshEnabled, LocalizationManager.T("tooltip.refresh_enabled"));
            }
            if (this.txtRefreshCmd != null)
            {
                this.timerToolTip.SetToolTip(this.txtRefreshCmd, LocalizationManager.T("tooltip.refresh_cmd"));
            }
            if (this.chkRefreshPauseSit != null)
            {
                this.timerToolTip.SetToolTip(this.chkRefreshPauseSit, LocalizationManager.T("tooltip.refresh_pause_sit"));
            }
            if (this.txtRefreshHotkey != null)
            {
                this.timerToolTip.SetToolTip(this.txtRefreshHotkey, LocalizationManager.T("tooltip.refresh_hotkey"));
            }
            if (this.btnClearRefreshHotkey != null)
            {
                this.timerToolTip.SetToolTip(this.btnClearRefreshHotkey, LocalizationManager.T("tooltip.refresh_clear"));
            }
            if (this.chkRefreshAutoInterval != null)
            {
                this.timerToolTip.SetToolTip(this.chkRefreshAutoInterval, LocalizationManager.T("tooltip.refresh_auto"));
            }
            if (this.numRefreshInterval != null)
            {
                this.timerToolTip.SetToolTip(this.numRefreshInterval, LocalizationManager.T("tooltip.refresh_interval"));
            }
            if (this.btnTestRefresh != null)
            {
                this.timerToolTip.SetToolTip(this.btnTestRefresh, LocalizationManager.T("tooltip.refresh_test"));
            }

            string delayTip = LocalizationManager.T("tooltip.timer_delay");
            string keyTip = LocalizationManager.T("tooltip.timer_key");
            string clearTip = LocalizationManager.T("tooltip.timer_clear");
            string removeTip = LocalizationManager.T("tooltip.timer_remove");

            for (int i = 0; i < this.slots.Length; i++)
            {
                TimerSlotUi slot = this.slots[i];
                if (slot == null)
                {
                    continue;
                }

                this.timerToolTip.SetToolTip(slot.DelayBox, delayTip);
                this.timerToolTip.SetToolTip(slot.KeyBox, keyTip);
                this.timerToolTip.SetToolTip(slot.BtnClear, clearTip);
                if (slot.BtnRemove != null)
                {
                    this.timerToolTip.SetToolTip(slot.BtnRemove, removeTip);
                }
            }

            if (this.btnAddTimer != null)
            {
                this.timerToolTip.SetToolTip(this.btnAddTimer, LocalizationManager.T("tooltip.timer_add"));
            }
        }

        private TimerSlotUi CreateTimerSlotUi(int slotNumber)
        {
            TimerSlotUi slot = new TimerSlotUi { SlotNumber = slotNumber };

            GroupBox group = new GroupBox
            {
                Name = "groupBox" + slotNumber,
                Size = new Size(CARD_WIDTH, CARD_HEIGHT),
                TabStop = false,
                Text = "Skill timer " + slotNumber
            };
            slot.Group = group;

            Label lblDelay = new Label
            {
                AutoSize = false,
                Font = this.labelFont,
                Location = new Point(4, 26),
                Name = "lblAutoRefreshDelay" + slotNumber,
                Size = new Size(52, 19),
                Text = "Delay",
                TextAlign = ContentAlignment.MiddleRight
            };

            NumericUpDown delayBox = new NumericUpDown
            {
                Font = this.inputFont,
                Location = new Point(60, 24),
                Maximum = new decimal(new int[] { 10000, 0, 0, 0 }),
                Minimum = 0,
                Name = slotNumber == 1 ? "txtAutoRefreshDelay" : "txtAutoRefreshDelay" + slotNumber,
                Size = new Size(58, 23),
                Value = 5
            };
            delayBox.ValueChanged += (s, e) => OnTimerDelayChanged(delayBox, GetSpammer(slotNumber));

            Label lblSec = new Label
            {
                AutoSize = true,
                Font = this.labelFont,
                Location = new Point(122, 28),
                Name = "lblAutoRefreshSec" + slotNumber,
                Text = "sec"
            };

            Label lblKey = new Label
            {
                AutoSize = false,
                Font = this.labelFont,
                Location = new Point(4, 56),
                Name = "lblAutoRefreshKey" + slotNumber,
                Size = new Size(52, 19),
                Text = "Key",
                TextAlign = ContentAlignment.MiddleRight
            };

            TextBox keyBox = new TextBox
            {
                Font = this.inputFont,
                Location = new Point(60, 54),
                Name = slotNumber == 1 ? "txtSkillTimerKey" : "txtSkillTimerKey" + slotNumber,
                Size = new Size(58, 23),
                Text = Key.None.ToString()
            };
            keyBox.KeyDown += new System.Windows.Forms.KeyEventHandler(FormUtils.OnKeyDown);
            keyBox.KeyPress += new KeyPressEventHandler(FormUtils.OnKeyPress);
            keyBox.TextChanged += (s, e) => OnTimerKeyChanged(keyBox, GetSpammer(slotNumber));

            Button btnClear = new Button
            {
                Cursor = System.Windows.Forms.Cursors.Hand,
                Location = new Point(10, 88),
                Name = "btnClearTimer" + slotNumber,
                Size = new Size(slotNumber == 1 ? 154 : 62, 22),
                Text = "Clear",
                UseVisualStyleBackColor = true
            };
            btnClear.Click += (s, e) => ClearTimer(slotNumber, false);

            slot.DelayBox = delayBox;
            slot.KeyBox = keyBox;
            slot.BtnClear = btnClear;

            group.Controls.Add(lblDelay);
            group.Controls.Add(delayBox);
            group.Controls.Add(lblSec);
            group.Controls.Add(lblKey);
            group.Controls.Add(keyBox);
            group.Controls.Add(btnClear);

            if (slotNumber > 1)
            {
                Button btnRemove = new Button
                {
                    Cursor = System.Windows.Forms.Cursors.Hand,
                    Location = new Point(78, 88),
                    Name = "btnRemoveTimer" + slotNumber,
                    Size = new Size(86, 22),
                    Text = "Remove Timer",
                    UseVisualStyleBackColor = true
                };
                btnRemove.Click += (s, e) => ClearTimer(slotNumber, true);
                slot.BtnRemove = btnRemove;
                group.Controls.Add(btnRemove);
            }

            return slot;
        }

        private void LayoutVisibleTimers()
        {
            this.SuspendLayout();
            try
            {
                int visibleIdx = 0;
                bool hasHidden = false;

                for (int i = 0; i < this.slots.Length; i++)
                {
                    TimerSlotUi slot = this.slots[i];
                    if (slot == null)
                    {
                        continue;
                    }

                    slot.Group.Visible = slot.IsVisible;
                    if (slot.IsVisible)
                    {
                        int row = visibleIdx / COLS_PER_ROW;
                        int col = visibleIdx % COLS_PER_ROW;
                        slot.Group.Location = new Point(GRID_START_X + (col * COL_STEP_X), GRID_START_Y + (row * ROW_STEP_Y));
                        visibleIdx++;
                    }
                    else
                    {
                        hasHidden = true;
                    }
                }

                if (this.btnAddTimer != null)
                {
                    int addRow = visibleIdx / COLS_PER_ROW;
                    int addCol = visibleIdx % COLS_PER_ROW;
                    int btnX = GRID_START_X + (addCol * COL_STEP_X);
                    int btnY = GRID_START_Y + (addRow * ROW_STEP_Y) + 46;
                    this.btnAddTimer.Location = new Point(btnX, btnY);
                    this.btnAddTimer.Visible = hasHidden;
                    this.btnAddTimer.Enabled = hasHidden;
                }
            }
            finally
            {
                this.ResumeLayout(true);
            }
        }

        private static AutoRefreshSpammer GetSpammer(int slotNumber)
        {
            Profile profile = ProfileSingleton.GetCurrent();
            switch (slotNumber)
            {
                case 1: return profile.AutoRefreshSpammer1;
                case 2: return profile.AutoRefreshSpammer2;
                case 3: return profile.AutoRefreshSpammer3;
                case 4: return profile.AutoRefreshSpammer4;
                case 5: return profile.AutoRefreshSpammer5;
                case 6: return profile.AutoRefreshSpammer6;
                default: return null;
            }
        }

        public bool AddNextTimer()
        {
            for (int i = 1; i < this.slots.Length; i++)
            {
                TimerSlotUi slot = this.slots[i];
                if (slot != null && !slot.IsVisible)
                {
                    slot.IsVisible = true;
                    LayoutVisibleTimers();
                    return true;
                }
            }

            return false;
        }

        public void ClearTimer(int slotNumber, bool hideSlot = false)
        {
            if (slotNumber < 1 || slotNumber > TOTAL_TIMER_SLOTS)
            {
                return;
            }

            TimerSlotUi slot = this.slots[slotNumber - 1];
            AutoRefreshSpammer spammer = GetSpammer(slotNumber);

            this.isUpdatingUi = true;
            try
            {
                slot.KeyBox.Text = Key.None.ToString();
                FormUtils.SetNumericValue(slot.DelayBox, 5);
            }
            finally
            {
                this.isUpdatingUi = false;
            }

            if (spammer != null)
            {
                spammer.RefreshKey = Key.None;
                spammer.RefreshDelay = 5;
                if (!ProfileSingleton.IsSaveSuppressed)
                {
                    ProfileSingleton.SetConfiguration(spammer);
                }
            }

            if (hideSlot && slotNumber > 1)
            {
                slot.IsVisible = false;
                LayoutVisibleTimers();
            }
        }

        public bool IsTimerVisible(int slotNumber)
        {
            if (slotNumber < 1 || slotNumber > TOTAL_TIMER_SLOTS)
            {
                return false;
            }

            return this.slots[slotNumber - 1] != null && this.slots[slotNumber - 1].IsVisible;
        }

        private void SyncFromProfile()
        {
            this.isUpdatingUi = true;
            try
            {
                Profile current = ProfileSingleton.GetCurrent();
                if (current != null && current.AutoRefreshCommand != null && this.chkRefreshEnabled != null)
                {
                    AutoRefreshCommand refresh = current.AutoRefreshCommand;
                    this.chkRefreshEnabled.Checked = refresh.Enabled;
                    this.txtRefreshCmd.Text = AutoRefreshCommand.NormalizeCommand(refresh.CommandText);
                    this.chkRefreshPauseSit.Checked = refresh.PauseWhenSitting;
                    this.txtRefreshHotkey.Text = refresh.Hotkey.ToString();
                    this.chkRefreshAutoInterval.Checked = refresh.AutoIntervalEnabled;
                    FormUtils.SetNumericValue(this.numRefreshInterval, refresh.GetEffectiveIntervalSeconds());
                    this.numRefreshInterval.Enabled = refresh.AutoIntervalEnabled;
                }

                for (int i = 0; i < TOTAL_TIMER_SLOTS; i++)
                {
                    int slotNum = i + 1;
                    TimerSlotUi slot = this.slots[i];
                    AutoRefreshSpammer spammer = GetSpammer(slotNum);
                    if (slot == null || spammer == null)
                    {
                        continue;
                    }

                    slot.KeyBox.Text = spammer.RefreshKey.ToString();
                    FormUtils.SetNumericValue(slot.DelayBox, spammer.RefreshDelay);
                    slot.IsVisible = (slotNum == 1) || (spammer.RefreshKey != Key.None);
                }

                LayoutVisibleTimers();
            }
            finally
            {
                this.isUpdatingUi = false;
            }
        }

        public void Update(ISubject subject)
        {
            Subject subj = subject as Subject;
            if (subj == null || subj.Message == null)
            {
                return;
            }

            switch (subj.Message.code)
            {
                case MessageCode.PROFILE_CHANGED:
                    SyncFromProfile();
                    break;
                case MessageCode.TURN_ON:
                    Profile currentOn = ProfileSingleton.GetCurrent();
                    currentOn.AutoRefreshSpammer1.Start();
                    currentOn.AutoRefreshSpammer2.Start();
                    currentOn.AutoRefreshSpammer3.Start();
                    currentOn.AutoRefreshSpammer4.Start();
                    currentOn.AutoRefreshSpammer5.Start();
                    currentOn.AutoRefreshSpammer6.Start();
                    if (currentOn.AutoRefreshCommand != null)
                    {
                        currentOn.AutoRefreshCommand.Start();
                    }
                    break;
                case MessageCode.TURN_OFF:
                    Profile currentOff = ProfileSingleton.GetCurrent();
                    currentOff.AutoRefreshSpammer1.Stop();
                    currentOff.AutoRefreshSpammer2.Stop();
                    currentOff.AutoRefreshSpammer3.Stop();
                    currentOff.AutoRefreshSpammer4.Stop();
                    currentOff.AutoRefreshSpammer5.Stop();
                    currentOff.AutoRefreshSpammer6.Stop();
                    if (currentOff.AutoRefreshCommand != null)
                    {
                        currentOff.AutoRefreshCommand.Stop();
                    }
                    break;
                case MessageCode.LANGUAGE_CHANGED:
                    RefreshTooltips();
                    break;
            }
        }

        private void OnTimerKeyChanged(TextBox keyBox, AutoRefreshSpammer spammer)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed || spammer == null)
            {
                return;
            }

            Key key;
            if (KeyHelper.TryParseKey(keyBox.Text, out key))
            {
                spammer.RefreshKey = key;
                ProfileSingleton.SetConfiguration(spammer);
            }
        }

        private void OnTimerDelayChanged(NumericUpDown delayBox, AutoRefreshSpammer spammer)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed || spammer == null)
            {
                return;
            }

            spammer.RefreshDelay = decimal.ToInt32(delayBox.Value);
            ProfileSingleton.SetConfiguration(spammer);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (this.labelFont != null)
            {
                this.labelFont.Dispose();
                this.labelFont = null;
            }
            if (this.inputFont != null)
            {
                this.inputFont.Dispose();
                this.inputFont = null;
            }
            if (this.hintFont != null)
            {
                this.hintFont.Dispose();
                this.hintFont = null;
            }
            base.OnFormClosed(e);
        }
    }
}
