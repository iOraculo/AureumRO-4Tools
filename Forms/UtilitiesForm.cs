using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Input;
using _4RTools.Model;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public class UtilitiesForm : Form, IObserver
    {
        private readonly ToolTip helpToolTip;
        private readonly Font hintFont;
        private readonly Font infoFont;

        private bool isUpdatingUi;

        private GroupBox gbAntiPositionBug;
        private CheckBox chkRefreshEnabled;
        private Label lblRefreshCmd;
        private TextBox txtRefreshCmd;
        private Button btnTestRefresh;
        private Label lblRefreshHotkey;
        private TextBox txtRefreshHotkey;
        private Button btnClearRefreshHotkey;
        private CheckBox chkRefreshAutoInterval;
        private NumericUpDown numRefreshInterval;
        private Label lblRefreshIntervalSec;
        private CheckBox chkRefreshPauseSit;
        private Label lblRefreshHint;

        private GroupBox gbUtilitiesInfo;
        private Label lblUtilitiesInfoBody;

        public UtilitiesForm(Subject subject)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.Name = "UtilitiesForm";
            this.Text = "Utilities";
            this.Size = new Size(584, 281);
            this.helpToolTip = FormUtils.CreateHelpToolTip();
            this.hintFont = new Font("Segoe UI", 8F, FontStyle.Regular);
            this.infoFont = new Font("Segoe UI", 8.25F, FontStyle.Regular);

            BuildUi();
            RefreshTooltipsAndDynamicText();
            SyncFromProfile();

            if (subject != null)
            {
                subject.Attach(this);
            }
        }

        private void BuildUi()
        {
            this.SuspendLayout();
            try
            {
                // Card 1: Anti-Position Bug (@refresh)
                this.gbAntiPositionBug = new GroupBox
                {
                    Location = new Point(8, 6),
                    Name = "gbAntiPositionBug",
                    Size = new Size(568, 146),
                    TabStop = false,
                    Text = "Anti-Position Bug (@refresh)"
                };

                this.chkRefreshEnabled = new CheckBox
                {
                    AutoSize = true,
                    Cursor = System.Windows.Forms.Cursors.Hand,
                    Location = new Point(14, 24),
                    Name = "chkRefreshEnabled",
                    Text = "Enable @refresh",
                    UseVisualStyleBackColor = true
                };
                this.chkRefreshEnabled.CheckedChanged += OnRefreshSettingsChanged;

                this.lblRefreshCmd = new Label
                {
                    AutoSize = true,
                    Location = new Point(160, 25),
                    Name = "lblRefreshCmd",
                    Text = "Command:"
                };

                this.txtRefreshCmd = new TextBox
                {
                    Location = new Point(232, 22),
                    MaxLength = 40,
                    Name = "txtRefreshCmd",
                    Size = new Size(120, 20),
                    Tag = FormUtils.SKIP_RESET_TAG,
                    Text = AutoRefreshCommand.DEFAULT_COMMAND
                };
                this.txtRefreshCmd.Leave += OnRefreshCommandLeave;
                this.txtRefreshCmd.TextChanged += OnRefreshSettingsChanged;

                this.btnTestRefresh = new Button
                {
                    Cursor = System.Windows.Forms.Cursors.Hand,
                    Location = new Point(468, 20),
                    Name = "btnTestRefresh",
                    Size = new Size(84, 24),
                    Text = "Test",
                    UseVisualStyleBackColor = true
                };
                this.btnTestRefresh.Click += OnTestRefreshClick;

                this.lblRefreshHotkey = new Label
                {
                    AutoSize = true,
                    Location = new Point(14, 59),
                    Name = "lblRefreshHotkey",
                    Text = "Hotkey (1-tap):"
                };

                this.txtRefreshHotkey = new TextBox
                {
                    Location = new Point(128, 56),
                    Name = "txtRefreshHotkey",
                    Size = new Size(72, 20),
                    Text = Key.None.ToString(),
                    TextAlign = HorizontalAlignment.Center
                };
                this.txtRefreshHotkey.KeyDown += FormUtils.OnKeyDown;
                this.txtRefreshHotkey.KeyPress += FormUtils.OnKeyPress;
                this.txtRefreshHotkey.TextChanged += OnRefreshHotkeyChanged;

                this.btnClearRefreshHotkey = new Button
                {
                    Cursor = System.Windows.Forms.Cursors.Hand,
                    Location = new Point(206, 55),
                    Name = "btnClearRefreshHotkey",
                    Size = new Size(64, 23),
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
                    Location = new Point(14, 92),
                    Name = "chkRefreshAutoInterval",
                    Text = "Auto every:",
                    UseVisualStyleBackColor = true
                };
                this.chkRefreshAutoInterval.CheckedChanged += OnRefreshSettingsChanged;

                this.numRefreshInterval = new NumericUpDown
                {
                    Location = new Point(128, 90),
                    Maximum = AutoRefreshCommand.MAX_INTERVAL_SECONDS,
                    Minimum = AutoRefreshCommand.MIN_INTERVAL_SECONDS,
                    Name = "numRefreshInterval",
                    Size = new Size(72, 20),
                    Tag = FormUtils.SKIP_RESET_TAG,
                    Value = AutoRefreshCommand.DEFAULT_INTERVAL_SECONDS
                };
                this.numRefreshInterval.ValueChanged += OnRefreshSettingsChanged;

                this.lblRefreshIntervalSec = new Label
                {
                    AutoSize = true,
                    Location = new Point(206, 93),
                    Name = "lblRefreshIntervalSec",
                    Text = "sec"
                };

                this.chkRefreshPauseSit = new CheckBox
                {
                    AutoSize = true,
                    Checked = true,
                    Cursor = System.Windows.Forms.Cursors.Hand,
                    Location = new Point(268, 92),
                    Name = "chkRefreshPauseSit",
                    Text = "Pause when sitting",
                    UseVisualStyleBackColor = true
                };
                this.chkRefreshPauseSit.CheckedChanged += OnRefreshSettingsChanged;

                this.lblRefreshHint = new Label
                {
                    AutoSize = false,
                    Font = this.hintFont,
                    Location = new Point(14, 120),
                    Name = "lblRefreshHint",
                    Size = new Size(540, 18),
                    Tag = ThemeManager.TAG_ACCENT_TEXT,
                    TextAlign = ContentAlignment.MiddleLeft
                };

                this.gbAntiPositionBug.Controls.Add(this.chkRefreshEnabled);
                this.gbAntiPositionBug.Controls.Add(this.lblRefreshCmd);
                this.gbAntiPositionBug.Controls.Add(this.txtRefreshCmd);
                this.gbAntiPositionBug.Controls.Add(this.btnTestRefresh);
                this.gbAntiPositionBug.Controls.Add(this.lblRefreshHotkey);
                this.gbAntiPositionBug.Controls.Add(this.txtRefreshHotkey);
                this.gbAntiPositionBug.Controls.Add(this.btnClearRefreshHotkey);
                this.gbAntiPositionBug.Controls.Add(this.chkRefreshAutoInterval);
                this.gbAntiPositionBug.Controls.Add(this.numRefreshInterval);
                this.gbAntiPositionBug.Controls.Add(this.lblRefreshIntervalSec);
                this.gbAntiPositionBug.Controls.Add(this.chkRefreshPauseSit);
                this.gbAntiPositionBug.Controls.Add(this.lblRefreshHint);

                // Card 2: How it works
                this.gbUtilitiesInfo = new GroupBox
                {
                    Location = new Point(8, 160),
                    Name = "gbUtilitiesInfo",
                    Size = new Size(568, 108),
                    TabStop = false,
                    Text = "How it works"
                };

                this.lblUtilitiesInfoBody = new Label
                {
                    AutoSize = false,
                    Font = this.infoFont,
                    Location = new Point(14, 20),
                    Name = "lblUtilitiesInfoBody",
                    Size = new Size(540, 80),
                    Tag = ThemeManager.TAG_MUTED_TEXT
                };

                this.gbUtilitiesInfo.Controls.Add(this.lblUtilitiesInfoBody);

                this.Controls.Add(this.gbAntiPositionBug);
                this.Controls.Add(this.gbUtilitiesInfo);
            }
            finally
            {
                this.ResumeLayout(true);
            }
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

        private void RefreshTooltipsAndDynamicText()
        {
            if (this.lblRefreshHint != null)
            {
                this.lblRefreshHint.Text = LocalizationManager.T("refresh.hint");
            }

            if (this.lblUtilitiesInfoBody != null)
            {
                this.lblUtilitiesInfoBody.Text = LocalizationManager.T("utilities.info_body");
            }

            if (this.helpToolTip == null)
            {
                return;
            }

            this.helpToolTip.SetToolTip(this.chkRefreshEnabled, LocalizationManager.T("tooltip.refresh_enabled"));
            this.helpToolTip.SetToolTip(this.txtRefreshCmd, LocalizationManager.T("tooltip.refresh_cmd"));
            this.helpToolTip.SetToolTip(this.chkRefreshPauseSit, LocalizationManager.T("tooltip.refresh_pause_sit"));
            this.helpToolTip.SetToolTip(this.txtRefreshHotkey, LocalizationManager.T("tooltip.refresh_hotkey"));
            this.helpToolTip.SetToolTip(this.btnClearRefreshHotkey, LocalizationManager.T("tooltip.refresh_clear"));
            this.helpToolTip.SetToolTip(this.chkRefreshAutoInterval, LocalizationManager.T("tooltip.refresh_auto"));
            this.helpToolTip.SetToolTip(this.numRefreshInterval, LocalizationManager.T("tooltip.refresh_interval"));
            this.helpToolTip.SetToolTip(this.btnTestRefresh, LocalizationManager.T("tooltip.refresh_test"));
        }

        private void SyncFromProfile()
        {
            this.isUpdatingUi = true;
            try
            {
                Profile current = ProfileSingleton.GetCurrent();
                if (current != null && current.AutoRefreshCommand != null)
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
                    if (currentOn != null && currentOn.AutoRefreshCommand != null)
                    {
                        currentOn.AutoRefreshCommand.Start();
                    }
                    break;
                case MessageCode.TURN_OFF:
                    Profile currentOff = ProfileSingleton.GetCurrent();
                    if (currentOff != null && currentOff.AutoRefreshCommand != null)
                    {
                        currentOff.AutoRefreshCommand.Stop();
                    }
                    break;
                case MessageCode.LANGUAGE_CHANGED:
                    RefreshTooltipsAndDynamicText();
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.helpToolTip != null)
                {
                    this.helpToolTip.Dispose();
                }
                if (this.hintFont != null)
                {
                    this.hintFont.Dispose();
                }
                if (this.infoFont != null)
                {
                    this.infoFont.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }
}
