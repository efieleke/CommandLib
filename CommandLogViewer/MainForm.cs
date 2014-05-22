using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CommandLogViewer
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void openBtn_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Multiselect = false;

                if (openFileDialog.ShowDialog(this) == DialogResult.OK)
                {
                    childList.Items.Clear();
                    parentList.Items.Clear();
                    commandList.Items.Clear();
                    listData.Clear();

                    // Open the selected file to read.
                    System.IO.FileStream stream = new System.IO.FileStream(openFileDialog.FileName, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);

                    using (System.IO.StreamReader fileStream = new System.IO.StreamReader(stream))
                    {
                        for (String line = fileStream.ReadLine(); line != null; line = fileStream.ReadLine())
                        {
                            String[] fields = line.Split(new char[]{' '}, int.MaxValue, StringSplitOptions.RemoveEmptyEntries);
                            DateTime time = DateTime.Parse(fields[0], null, System.Globalization.DateTimeStyles.RoundtripKind);
                            String idText = fields[1];
                            String[] ids = fields[1].Split(new char[] { '(', ')' });
                            long id = int.Parse(ids[0]);
                            long parentId = int.Parse(ids[1]);
                            String action = fields[2];
                            String type = fields[3];
                            String details = "";

                            for (int i = 4; i < fields.Count(); ++i)
                            {
                                details += (fields[i] + " ");
                            }

                            if (action == "Starting")
                            {
                                listData.Add(new ListData(type, time, DateTime.MaxValue, action, details, id, parentId, new List<long>()));
                            }
                            else
                            {
                                foreach(ListData data in listData)
                                {
                                    if (data.id == id && data.status == "Starting") // Commands can be executed more than once
                                    {
                                        data.status = action;
                                        data.finishTime = time;
                                        data.details = details;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    foreach(ListData data in listData)
                    {
                        if (data.parentId != 0)
                        {
                            foreach(ListData data2 in listData)
                            {
                                if (data2.id == data.parentId)
                                {
                                    if (!data2.childIds.Contains(data.id))
                                    {
                                        data2.childIds.Add(data.id);
                                    }
                                }
                            }
                        }

                        AddListItem(commandList, data);
                    }

                    ResizeColumns(commandList);
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(this, exc.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        static private void AddListItem(ListView listView, ListData data)
        {
            ListViewItem item = new ListViewItem(data.commandType);
            item.Tag = data;
            item.SubItems.Add(data.id.ToString());
            item.SubItems.Add(data.startTime.ToString("o"));
            item.SubItems.Add(data.status == "Starting" ? "Running" : data.status + " at " + data.finishTime.ToString("o"));
            item.SubItems.Add(data.details);
            listView.Items.Add(item);
        }

        static private void ResizeColumns(ListView listView)
        {
            listView.AutoResizeColumns(listView.Items.Count > 0 ? ColumnHeaderAutoResizeStyle.ColumnContent : ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        private ListData FindListDataEncompassing(int upperBound, long commandId, DateTime start, DateTime finish)
        {
            for (int i = 0; i < upperBound; ++i )
            {
                ListData data = listData[i];

                if (data.id == commandId && data.finishTime >= finish)
                {
                    return data;
                }
            }

            return null;
        }

        private List<ListData> FindListDataWithin(int lowerBound, long commandId, DateTime start, DateTime finish)
        {
            List<ListData> result = new List<ListData>();

            for (int i = lowerBound + 1; i < listData.Count; ++i)
            {
                ListData data = listData[i];

                if (data.id == commandId)
                {
                    if (data.finishTime > finish)
                    {
                        break;
                    }

                    result.Add(data);
                }
            }

            return result;
        }

        private void commandList_SelectedIndexChanged(object sender, EventArgs e)
        {
            parentList.Items.Clear();
            childList.Items.Clear();

            if (commandList.SelectedItems.Count == 1)
            {
                int selectedIndex = commandList.SelectedItems[0].Index;
                ListData listData = (ListData)commandList.SelectedItems[0].Tag;
                ListData parentData = FindListDataEncompassing(selectedIndex, listData.parentId, listData.startTime, listData.finishTime);

                if (parentData != null)
                {
                    AddListItem(parentList, parentData);
                }

                foreach (long childId in listData.childIds)
                {
                    List<ListData> childData = FindListDataWithin(selectedIndex, childId, listData.startTime, listData.finishTime);

                    foreach (ListData child in childData)
                    {
                        AddListItem(childList, child);
                    }
                }

                ResizeColumns(parentList);
                ResizeColumns(childList);
            }
        }

        private void SelectReferencedItem(ListView listView)
        {
            if (listView.SelectedItems.Count == 1)
            {
                ListData listData = (ListData)listView.SelectedItems[0].Tag;

                foreach (ListViewItem item in commandList.Items)
                {
                    if (item.Tag == listData)
                    {
                        commandList.Focus();
                        item.Selected = true;
                        commandList.EnsureVisible(item.Index);
                        break;
                    }
                }
            }

        }

        private void parentList_ItemActivate(object sender, EventArgs e)
        {
            SelectReferencedItem(parentList);
        }

        private void childList_ItemActivate(object sender, EventArgs e)
        {
            SelectReferencedItem(childList);
        }

        private void exitBtn_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            ResizeColumns(commandList);
            ResizeColumns(parentList);
            ResizeColumns(childList);
        }

        private class ListData
        {
            internal ListData(String commandType, DateTime startTime, DateTime finishTime, String status, String details, long id, long parentId, List<long> childIds)
            {
                this.commandType = commandType;
                this.startTime = startTime;
                this.finishTime = finishTime;
                this.status = status;
                this.details = details;
                this.id = id;
                this.parentId = parentId;
                this.childIds = childIds;
            }

            internal String commandType;
            internal DateTime startTime;
            internal DateTime finishTime;
            internal String status;
            internal String details;
            internal long id;
            internal long parentId;
            internal List<long> childIds;
        }

        private List<ListData> listData = new List<ListData>();
    }
}
