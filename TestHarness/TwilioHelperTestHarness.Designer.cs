namespace TestHarness
{
  partial class TwilioHelperTestHarness
  {
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
      if (disposing && (components != null))
      {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      this.serviceBusListenerParameters1 = new ServiceBusListener.TwiddlerControl();
      this.SuspendLayout();
      // 
      // serviceBusListenerParameters1
      // 
      this.serviceBusListenerParameters1.Dock = System.Windows.Forms.DockStyle.Fill;
      this.serviceBusListenerParameters1.Location = new System.Drawing.Point(0, 0);
      this.serviceBusListenerParameters1.Name = "serviceBusListenerParameters1";
      this.serviceBusListenerParameters1.Size = new System.Drawing.Size(784, 562);
      this.serviceBusListenerParameters1.TabIndex = 0;
      // 
      // TwilioHelperTestHarness
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(784, 562);
      this.Controls.Add(this.serviceBusListenerParameters1);
      this.Name = "TwilioHelperTestHarness";
      this.Text = "Twiddler Test Harness";
      this.ResumeLayout(false);

    }

    #endregion

    private ServiceBusListener.TwiddlerControl serviceBusListenerParameters1;
  }
}

