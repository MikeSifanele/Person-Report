using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Excel;
using System.Windows.Forms;

namespace The_Hive
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        FileSystemWatcher fswDirectoryWatcher = null;
        StreamReader myReader = null;
        string[] myReadLine;
        string myLastDate = "", myLeavingTime = "", myStartTime = "", myPerson = "";
        double mytotalhours = 0;

        //string avgStartTime = "", avgLeavingTime = "";

        private void Form1_Load(object sender, EventArgs e)
        {
            fswDirectoryWatcher = new FileSystemWatcher();
            fswDirectoryWatcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Attributes;

            fswDirectoryWatcher.Created += new FileSystemEventHandler(fswDirectoryWatcher_Changed);
            fswDirectoryWatcher.IncludeSubdirectories = true;

            rbWatchFolder.Enabled = false;
            rbWatchFolder.Visible = false;
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            pbStatus.Value = 0;

            try
            {
                dgvDisplay.Controls.Clear();
                dgvDisplay.Refresh();

                using (OpenFileDialog ImportDialog = new OpenFileDialog())
                {
                    DialogResult myResult = ImportDialog.ShowDialog();

                    if (myResult == DialogResult.OK)
                    {
                        GetDataFromFile(ImportDialog.FileName);
                    }
                    else
                    {
                        MessageBox.Show("CSV file not imported.\nPlease try again", "Error");
                    }
                }
            }
            catch
            {
                MessageBox.Show("CSV file not imported.\nPlease close the file you are trying to import and try again", "Error - file open");

            }

        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            pbStatus.Value = 0;

            Write_Excel_File(myPerson);

            pbStatus.Value = pbStatus.Maximum;
        }

        private void rbWatchFolder_CheckedChanged(object sender, EventArgs e)
        {
            if (rbWatchFolder.Checked)
            {
                try
                {
                    using (FolderBrowserDialog ImportDialog = new FolderBrowserDialog())
                    {
                        DialogResult myResult = ImportDialog.ShowDialog();

                        if (myResult == DialogResult.OK)
                        {
                            fswDirectoryWatcher.Path = ImportDialog.SelectedPath;

                            fswDirectoryWatcher.EnableRaisingEvents = true;
                        }
                        else
                        {
                            MessageBox.Show("Folder to watch not specified.\nPlease try again", "Error");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Folder to watch not specified.\n{ex.Message}", "Error - file open");

                }
            }
            else
            {
                fswDirectoryWatcher.EnableRaisingEvents = false;
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void btnImport_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                string[] myFile = (string[])e.Data.GetData(DataFormats.FileDrop);

                GetDataFromFile(myFile[0]);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error - Droping file");
            }
            
        }

        private void btnImport_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void fswDirectoryWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            GetDataFromFile(e.FullPath);

        }
        private void GetDataFromFile(string myFilePath)
        {
            myStartTime = "";
            myLastDate = "";
            myLeavingTime = "";
            mytotalhours = 0;

            myReader = new StreamReader(myFilePath);

            myReader.ReadLine();
            myReader.ReadLine();
            myPerson = myReader.ReadLine().Split(',')[0].Trim('"').Replace(':', '-');
            myReader.ReadLine();
            myReader.ReadLine();

            while (myReader.EndOfStream == false)
            {
                myReadLine = myReader.ReadLine().Split(',');

                if (myLastDate == "")
                {
                    myLastDate = myReadLine[0].Trim('"');
                    myStartTime = myReadLine[1].Trim('"');
                }
                else if (myLastDate == myReadLine[0].Trim('"'))
                {
                    myLeavingTime = myReadLine[1].Trim('"');
                }
                else if (myLastDate != myReadLine[0].Trim('"'))
                {
                    dgvDisplay.Rows.Add(myLastDate.Substring(3), myStartTime, myLeavingTime, (Convert.ToDateTime(myLeavingTime) - Convert.ToDateTime(myStartTime)).TotalHours.ToString("f1"));
                    myLastDate = "";
                    mytotalhours += (Convert.ToDateTime(myLeavingTime) - Convert.ToDateTime(myStartTime)).TotalHours;
                }

                try
                {
                    if (pbStatus.Value < pbStatus.Maximum)
                        pbStatus.Increment(1);
                }
                catch
                {

                }
                
            }

            dgvDisplay.Rows.Add("", "", "", "Total Hours");
            dgvDisplay.Rows.Add("", "", "", mytotalhours.ToString("f1"));
            myReader.Close();

            try
            {

                pbStatus.Value = pbStatus.Maximum;
            }
            catch
            {

            }
        }

        public void Write_Excel_File(string myPersonName)
        {
            try
            {
                pbStatus.Value = 0;
            }
            catch
            {

            }

            _Application myApp = new Microsoft.Office.Interop.Excel.Application();
            _Workbook myWorkbook = myApp.Workbooks.Add(Type.Missing);
            _Worksheet myWorksheet = null;

            myWorksheet = myWorkbook.Sheets["Sheet1"];
            myWorksheet = myWorkbook.ActiveSheet;
            myWorksheet.Name = myPersonName.Substring(10).Split('(')[0].Trim(' ');

            myApp.Visible = true;

            for (int i = 1; i < dgvDisplay.Columns.Count + 1; i++)
            {
                myWorksheet.Cells[1, i] = dgvDisplay.Columns[i - 1].HeaderText;
            }

            for (int r = 0; r < dgvDisplay.Rows.Count; r++)
            {
                for (int c = 0; c < dgvDisplay.Columns.Count; c++)
                {
                    try
                    {
                        myWorksheet.Cells[r + 2, c + 1] = dgvDisplay.Rows[r].Cells[c].Value.ToString();

                        if (pbStatus.Value < 100)
                        {
                            pbStatus.Increment(1);
                        }
                    }
                    catch
                    {
                        break;
                    }
                }
            }

            var saveFileDialoge = new SaveFileDialog();
            saveFileDialoge.FileName = myPerson;
            saveFileDialoge.DefaultExt = ".xlsx";

            if (saveFileDialoge.ShowDialog() == DialogResult.OK)
            {
                myWorkbook.SaveAs(saveFileDialoge.FileName, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, XlSaveAsAccessMode.xlExclusive, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
            }

            myWorkbook.Close();
            myApp.Visible = false;

            try
            {
                pbStatus.Value = pbStatus.Maximum;
            }
            catch
            {

            }

        }
    }
}
