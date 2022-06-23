using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace LocalizationTool
{
    public partial class Form1 : Form
    {
        Localization.LocalizationFile _file = null;

        DataTable dataTable = new DataTable();

        int _currentLanguageId = 9;

        bool _generateHeader = false;

        string[] _template = null;

        public Form1()
        {
            InitializeComponent();
        }

        private void InitializeNewFile()
        {
            var items = new Dictionary<int, string>();

            int idx = 1;

            items.AddRange(Utils.Repeat(() =>
            {
                int key = idx++;
                return new KeyValuePair<int, string>(key, "");
            }, _template.Length)); // Fill the dictionary with empty values so when we serialize the file, they are in the correct order.

            _file = new Localization.LocalizationFile(2, _currentLanguageId, items);
        }

        private void InitializeTemplate()
        {
            if (File.Exists("eng.template"))
            {
                _template = File.ReadAllLines("eng.template");

                for (int i = 0; i < _template.Length; i++)
                {
                    var row = dataTable.NewRow();
                    row[0] = (i + 1).ToString();
                    row[1] = _template[i];
                    row[2] = "";
                    dataTable.Rows.Add(row);

                }

                dataTable.AcceptChanges();
            }
        }

        private void GenerateHeader()
        {
            if (_template == null)
                return;

            using (var file = new StreamWriter(Path.Combine(Environment.CurrentDirectory, "LocalizationTypes.h")))
            {
                file.WriteLine("#pragma once\n\n");

                file.WriteLine("enum StringID {\n");

                for (int i = 0; i < _template.Length; i++)
                {
                    var LocCodeName = "SID_" + _template[i].Replace(' ', '_').ToUpper();

                    file.WriteLine(LocCodeName + (i == _template.Length - 1 ? "\n" : ","));
                }

                file.WriteLine("};\n");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);

            comboBox1.Items.AddRange(cultures.OrderBy(c => c.LCID).Select(x => { return x.EnglishName + "(" + x.LCID + ")"; }).ToArray());

            comboBox1.SelectedIndex = _currentLanguageId;

            var column = dataTable.Columns.Add("ID");
            dataTable.PrimaryKey = new DataColumn[] { column };
            dataTable.Columns.Add("English Name");
            dataTable.Columns.Add("Localized Name");

            dataGridView1.DataSource = dataTable;

            InitializeTemplate();

            InitializeNewFile();

            dataGridView1.CellValidated += DataGridView1_CellValidated;

            checkBox1.Checked = _generateHeader;
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            var svd = new OpenFileDialog
            {
                Filter = "Cx Localization Files (*.loc)|*.loc|All files (*.*)|*.*",

                InitialDirectory = Environment.CurrentDirectory
            };

            if (svd.ShowDialog() != DialogResult.OK)
                return;

            dataTable.Rows.Clear();

            _file = new Localization.LocalizationFile();

            var bytes = File.ReadAllBytes(svd.FileName);

            _file.Deserialize(bytes);

            if (_file.Header.LanguageID == 0)
            {
                MessageBox.Show("Cannot load the file. Invalid language ID or its corrupt.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                return;
            }

            _currentLanguageId = _file.Header.LanguageID;

            comboBox1.SelectedIndex = _currentLanguageId;

            label4.Text = _file.Items.Count.ToString();

            label5.Text = new CultureInfo(_file.Header.LanguageID).EnglishName;

            label6.Text = _file.Header.Version.ToString();

            dataTable.Rows.Clear();

            InitializeTemplate();

            foreach (var item in _file.Items)
            {
                DataRow row = dataTable.Rows.Find(item.Key);

                bool isNew = false;

                if (row == null)
                {
                    row = dataTable.NewRow();
                    isNew = true;
                }

                row[0] = item.Key;
                row[1] = item.Key > _template.Length ? "" : _template[item.Key - 1];
                row[2] = item.Value;

                if (isNew)
                    dataTable.Rows.Add(row);
            }

            dataTable.AcceptChanges();

            dataGridView1.Refresh();   
        }

        private void button2_Click(object sender, EventArgs e)
        {
            dataTable.Rows.Clear();

            InitializeTemplate();

            InitializeNewFile(); 
        }

        private void DataGridView1_CellValidated(object sender, DataGridViewCellEventArgs e)
        {
            if (dataGridView1.RowCount <= 0 || dataGridView1.Rows[e.RowIndex].Cells[2].Value == null)
                return;

            if (_currentLanguageId == 9 && e.ColumnIndex == 1) // english, just copy the same value over.
                dataGridView1.Rows[e.RowIndex].Cells[2].Value = dataGridView1.Rows[e.RowIndex].Cells[1].Value;

            //var rowKey = e.RowIndex.ToString();// Convert.ToInt32(dataGridView1.Rows[e.RowIndex].Cells[0].Value);

            _file.Items[e.RowIndex + 1] = dataGridView1.Rows[e.RowIndex].Cells[2].Value.ToString();
        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            _currentLanguageId = comboBox1.SelectedIndex;

            if (_file != null)
            {
                _file.Header.LanguageID = _currentLanguageId;
            }

            label5.Text = new CultureInfo(_currentLanguageId).EnglishName;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (_file == null)
                return;

            if (_currentLanguageId == 0)
            {
                MessageBox.Show("Language ID must be specified first.");

                return;
            }

            _file.Header.LanguageID = _currentLanguageId;
        
            using (var sfd = new SaveFileDialog
            {
                Filter = "Cx Localization Files (*.loc)|*.loc|All files (*.*)|*.*",

                InitialDirectory = Environment.CurrentDirectory,

                FileName = new CultureInfo(_currentLanguageId).ThreeLetterISOLanguageName
            })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    using (BinaryWriter writer = new BinaryWriter(sfd.OpenFile()))
                    {
                        writer.Write(_file.Serialize());
                    }
                }
            }

            if (_generateHeader)
                GenerateHeader();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            _generateHeader = checkBox1.Checked;
        }
    }
}
