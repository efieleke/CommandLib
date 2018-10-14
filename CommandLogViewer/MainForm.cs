using System;
using System.Collections.Generic;
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
	        OpenFileDialog openFileDialog = new OpenFileDialog {Multiselect = false};

	        if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                _logFileName = openFileDialog.FileName;
                LoadLogFile();
                reloadBtn.Enabled = true;
            }
        }


        private void reloadBtn_Click(object sender, EventArgs e)
        {
            LoadLogFile();
        }

        private void LoadLogFile()
        {
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                childList.Items.Clear();
                parentList.Items.Clear();
                commandList.Items.Clear();
                _listData.Clear();

                // Open the selected file to read.
                System.IO.FileStream stream = new System.IO.FileStream(_logFileName, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);

                using (System.IO.StreamReader fileStream = new System.IO.StreamReader(stream))
                {
                    for (string line = fileStream.ReadLine(); line != null; line = fileStream.ReadLine())
                    {
                        string[] fields = line.Split(new[] { ' ' }, int.MaxValue, StringSplitOptions.RemoveEmptyEntries);
                        DateTime time = DateTime.Parse(fields[0], null, System.Globalization.DateTimeStyles.RoundtripKind);
	                    string[] ids = fields[1].Split(new[] { '(', ')' });
                        long id = int.Parse(ids[0]);
                        long parentId = int.Parse(ids[1]);
                        string action = fields[2];
                        string type = fields[3];
                        string details = "";

                        for (int i = 4; i < fields.Length; ++i)
                        {
                            details += (fields[i] + " ");
                        }

                        if (action == "Starting")
                        {
                            _listData.Add(new ListData(type, time, DateTime.MaxValue, action, details, id, parentId));
                        }
                        else
                        {
                            for (int i = _listData.Count - 1; i >= 0; --i)
                            {
                                ListData data = _listData[i];

                                if (data.Id == id && data.Status == "Starting") // Commands can be executed more than once
                                {
                                    data.Status = action;
                                    data.FinishTime = time;
                                    data.Details = details;
                                    break;
                                }
                            }
                        }
                    }
                }

                foreach (ListData data in _listData)
                {
                    if (data.ParentId != 0)
                    {
                        if (!_childMap.ContainsKey(data.ParentId))
                        {
	                        HashSet<long> children = new HashSet<long> {data.Id};
	                        _childMap.Add(data.ParentId, children);
                        }
                        else
                        {
                            _childMap[data.ParentId].Add(data.Id);
                        }
                    }

                    AddListItem(commandList, data);
                }

                ResizeColumns(commandList);
            }
            catch (Exception exc)
            {
                reloadBtn.Enabled = false;
                commandList.Items.Clear();
                MessageBox.Show(this, exc.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        static private void AddListItem(ListView listView, ListData data)
        {
	        ListViewItem item = new ListViewItem(data.CommandType) {Tag = data};
	        item.SubItems.Add(data.Id.ToString());
            item.SubItems.Add(data.StartTime.ToString("o"));
            item.SubItems.Add(data.Status == "Starting" ? "Running" : data.Status + " at " + data.FinishTime.ToString("o"));
            item.SubItems.Add(data.Details);
            listView.Items.Add(item);
        }

        private static void ResizeColumns(ListView listView)
        {
            listView.AutoResizeColumns(listView.Items.Count > 0 ? ColumnHeaderAutoResizeStyle.ColumnContent : ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        private ListData FindListDataEncompassing(int upperBound, long commandId, DateTime finish)
        {
            for (int i = 0; i < upperBound; ++i )
            {
                ListData data = _listData[i];

                if (data.Id == commandId && data.FinishTime >= finish)
                {
                    return data;
                }
            }

            return null;
        }

        private List<ListData> FindListDataWithin(int lowerBound, long commandId, DateTime finish)
        {
            List<ListData> result = new List<ListData>();

            for (int i = lowerBound + 1; i < _listData.Count; ++i)
            {
                ListData data = _listData[i];

                if (data.Id == commandId)
                {
                    if (data.FinishTime > finish)
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
                ListData parentData = FindListDataEncompassing(selectedIndex, listData.ParentId, listData.FinishTime);

                if (parentData != null)
                {
                    AddListItem(parentList, parentData);
                }

                if (_childMap.ContainsKey(listData.Id))
                {
                    foreach (long childId in _childMap[listData.Id])
                    {
                        List<ListData> childData = FindListDataWithin(selectedIndex, childId, listData.FinishTime);

                        foreach (ListData child in childData)
                        {
                            AddListItem(childList, child);
                        }
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
            internal ListData(string commandType, DateTime startTime, DateTime finishTime, string status, string details, long id, long parentId)
            {
                CommandType = commandType;
                StartTime = startTime;
                FinishTime = finishTime;
                Status = status;
                Details = details;
                Id = id;
                ParentId = parentId;
            }

            internal readonly string CommandType;
            internal readonly DateTime StartTime;
            internal DateTime FinishTime;
            internal string Status;
            internal string Details;
            internal readonly long Id;
            internal readonly long ParentId;
        }

        private readonly List<ListData> _listData = new List<ListData>();
        private readonly Dictionary<long, HashSet<long>> _childMap = new Dictionary<long, HashSet<long>>();
        private string _logFileName;
    }
}
