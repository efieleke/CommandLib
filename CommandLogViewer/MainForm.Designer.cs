namespace CommandLogViewer
{
    partial class MainForm
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
            this.label1 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.exitBtn = new System.Windows.Forms.Button();
            this.childList = new System.Windows.Forms.ListView();
            this.typeCol = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader9 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.startTimeCol = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.statusCol = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.detailsCol = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.openBtn = new System.Windows.Forms.Button();
            this.commandList = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.idColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.parentList = new System.Windows.Forms.ListView();
            this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.idCol = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader7 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader8 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 18);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(221, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "All command executions in order of start times";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(13, 532);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(460, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Commands initiated by the selected command execution. Double-click an item to sel" +
    "ect it above.";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 463);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(788, 13);
            this.label4.TabIndex = 4;
            this.label4.Text = "Initiator of the selected command execution. Double-click this item to select it " +
    "above. If no initiator is listed here, the selected execution was from a top-lev" +
    "el command.";
            // 
            // exitBtn
            // 
            this.exitBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.exitBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.exitBtn.Location = new System.Drawing.Point(823, 605);
            this.exitBtn.Name = "exitBtn";
            this.exitBtn.Size = new System.Drawing.Size(75, 23);
            this.exitBtn.TabIndex = 9;
            this.exitBtn.Text = "Exit";
            this.exitBtn.UseVisualStyleBackColor = true;
            this.exitBtn.Click += new System.EventHandler(this.exitBtn_Click);
            // 
            // childList
            // 
            this.childList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.childList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.typeCol,
            this.columnHeader9,
            this.startTimeCol,
            this.statusCol,
            this.detailsCol});
            this.childList.FullRowSelect = true;
            this.childList.GridLines = true;
            this.childList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.childList.Location = new System.Drawing.Point(15, 548);
            this.childList.MultiSelect = false;
            this.childList.Name = "childList";
            this.childList.Size = new System.Drawing.Size(883, 50);
            this.childList.TabIndex = 7;
            this.childList.UseCompatibleStateImageBehavior = false;
            this.childList.View = System.Windows.Forms.View.Details;
            this.childList.ItemActivate += new System.EventHandler(this.childList_ItemActivate);
            // 
            // typeCol
            // 
            this.typeCol.Text = "Command Type";
            this.typeCol.Width = 90;
            // 
            // columnHeader9
            // 
            this.columnHeader9.Text = "ID";
            // 
            // startTimeCol
            // 
            this.startTimeCol.Text = "Start Time";
            // 
            // statusCol
            // 
            this.statusCol.Text = "Status";
            // 
            // detailsCol
            // 
            this.detailsCol.Text = "Details";
            // 
            // openBtn
            // 
            this.openBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.openBtn.Location = new System.Drawing.Point(16, 604);
            this.openBtn.Name = "openBtn";
            this.openBtn.Size = new System.Drawing.Size(102, 23);
            this.openBtn.TabIndex = 8;
            this.openBtn.Text = "Open Log File...";
            this.openBtn.UseVisualStyleBackColor = true;
            this.openBtn.Click += new System.EventHandler(this.openBtn_Click);
            // 
            // commandList
            // 
            this.commandList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.commandList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.idColumn,
            this.columnHeader2,
            this.columnHeader3,
            this.columnHeader4});
            this.commandList.FullRowSelect = true;
            this.commandList.GridLines = true;
            this.commandList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.commandList.HideSelection = false;
            this.commandList.Location = new System.Drawing.Point(12, 34);
            this.commandList.MultiSelect = false;
            this.commandList.Name = "commandList";
            this.commandList.Size = new System.Drawing.Size(883, 419);
            this.commandList.TabIndex = 1;
            this.commandList.UseCompatibleStateImageBehavior = false;
            this.commandList.View = System.Windows.Forms.View.Details;
            this.commandList.SelectedIndexChanged += new System.EventHandler(this.commandList_SelectedIndexChanged);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Command Type";
            this.columnHeader1.Width = 90;
            // 
            // idColumn
            // 
            this.idColumn.Text = "ID";
            this.idColumn.Width = 25;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Start Time";
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "Status";
            this.columnHeader3.Width = 45;
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "Details";
            this.columnHeader4.Width = 50;
            // 
            // parentList
            // 
            this.parentList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.parentList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader5,
            this.idCol,
            this.columnHeader6,
            this.columnHeader7,
            this.columnHeader8});
            this.parentList.FullRowSelect = true;
            this.parentList.GridLines = true;
            this.parentList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.parentList.Location = new System.Drawing.Point(15, 479);
            this.parentList.MultiSelect = false;
            this.parentList.Name = "parentList";
            this.parentList.Size = new System.Drawing.Size(883, 45);
            this.parentList.TabIndex = 5;
            this.parentList.UseCompatibleStateImageBehavior = false;
            this.parentList.View = System.Windows.Forms.View.Details;
            this.parentList.ItemActivate += new System.EventHandler(this.parentList_ItemActivate);
            // 
            // columnHeader5
            // 
            this.columnHeader5.Text = "Command Type";
            this.columnHeader5.Width = 90;
            // 
            // idCol
            // 
            this.idCol.Text = "ID";
            this.idCol.Width = 25;
            // 
            // columnHeader6
            // 
            this.columnHeader6.Text = "Start Time";
            // 
            // columnHeader7
            // 
            this.columnHeader7.Text = "Status";
            this.columnHeader7.Width = 46;
            // 
            // columnHeader8
            // 
            this.columnHeader8.Text = "Details";
            this.columnHeader8.Width = 50;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.exitBtn;
            this.ClientSize = new System.Drawing.Size(911, 635);
            this.Controls.Add(this.parentList);
            this.Controls.Add(this.commandList);
            this.Controls.Add(this.openBtn);
            this.Controls.Add(this.childList);
            this.Controls.Add(this.exitBtn);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label1);
            this.Name = "MainForm";
            this.Text = "Command Log Viewer";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button exitBtn;
        private System.Windows.Forms.ListView childList;
        private System.Windows.Forms.Button openBtn;
        private System.Windows.Forms.ColumnHeader typeCol;
        private System.Windows.Forms.ColumnHeader startTimeCol;
        private System.Windows.Forms.ColumnHeader statusCol;
        private System.Windows.Forms.ColumnHeader detailsCol;
        private System.Windows.Forms.ListView commandList;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.ListView parentList;
        private System.Windows.Forms.ColumnHeader columnHeader5;
        private System.Windows.Forms.ColumnHeader columnHeader6;
        private System.Windows.Forms.ColumnHeader columnHeader7;
        private System.Windows.Forms.ColumnHeader columnHeader8;
        private System.Windows.Forms.ColumnHeader columnHeader9;
        private System.Windows.Forms.ColumnHeader idColumn;
        private System.Windows.Forms.ColumnHeader idCol;
    }
}

