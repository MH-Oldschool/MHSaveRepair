using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MHSavRepair
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            Text = "Loading...";

            int filter = 0;
            string filename = "";
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "mem file|BISLPM-65495MH;BISLPM-65869MHG;BISLPM-66280MH2";
                openFileDialog.FilterIndex = 0; //to save me some clicks
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //Get the path of specified file
                    filename = openFileDialog.FileName;
                    filter = openFileDialog.FilterIndex;
                }
            }
            if (filename == "")
            {
                return;
            }
            
            List<ushort> buffer = new List<ushort>();
            using(BinaryReader br = new BinaryReader(File.Open(filename, FileMode.Open)))
            {
                for(int i = 0; i < br.BaseStream.Length/2; i++)
                buffer.Add(br.ReadUInt16());
            }

            ushort majick = buffer[0];
            ushort key = buffer[1];
            ushort csum = buffer[2];
            ushort dummy = buffer[3];
            uint length = 0;
            if(filename.EndsWith("MH") && majick == 0x100 && dummy == 0x5963)
            {
                //mh1!
                length = 0x8a20;
            }
            else if(filename.EndsWith("MHG") && majick == 0x100 && dummy == 0x5963)
            {
                //mhg!
                length = 0x9860;
            }
            else if(filename.EndsWith("MH2") && majick == 0x100)
            {
                //mh2!
                length = 0x550; //todo, verify
                Text = "Error";
                MessageBox.Show("MH2 needs more research!");
                return;
            }
            else
            {
                Text = "Error";
                MessageBox.Show("Unrecognized save data!");
                return;
            }
            ushort sum = 0;
            ushort changes = 0;
            for(int i = 0; i < length; i++)
            {
                ushort temp = buffer[4 + i];
                temp ^= key;
                if (i < 0x8100 && temp != 0)
                {
                    key = buffer[4 + i];
                    temp = 0;
                    changes++;
                }
                buffer[4 + i] = temp;
                sum += temp;
                if (key == 0)
                    key = 1;
                key = (ushort)(key * 0xb0 % 0xff53);
            }
            DialogResult result = MessageBox.Show("Number of fragments found: " + changes + "\nTry to repair?", "Status", MessageBoxButtons.YesNo);
            if (result == DialogResult.No)
            {
                Text = "Operations Aborted By User";
                return;
            }
            if (sum != csum)
                MessageBox.Show("Checksum mismatch!");
            //now reencade it all
            Text = "Saving...";
            key = buffer[1];
            sum = 0;
            for(int i = 0; i < length; i++)
            {
                sum += buffer[4 + i];
                buffer[4 + i] ^= key;
                key = (ushort)(key * 0xb0 % 0xff53);
            }
            buffer[2] = sum;
            using (BinaryWriter bw = new BinaryWriter(File.Create(Path.GetDirectoryName(filename) + "\\newsave")))
            {
                for (int i = 0; i < buffer.Count; i++)
                    bw.Write(buffer[i]);
            }
            Text = "Saved!";
            MessageBox.Show("Save attempt success");
        }

    }
}
