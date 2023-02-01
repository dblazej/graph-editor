using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Globalization;
using System.Threading;
using System.Xml.Serialization;

namespace GraphEditor
{
    public partial class Form1 : Form
    {
        private Color currentColor;
        private Bitmap drawArea;
        private const int RADIUS = 20;
        private const int neighborTolerant = 30;

        private bool checkedEllipse = false;
        private int indexCheckedEllipse;

        private bool movingEllipse = false;

        List<(Point point, Color color)> pointsList = new();
        List<(int pt1, int pt2)> linesList = new();

        public Form1()
        {
            InitializeComponent();
            drawArea = new Bitmap(canvas.Size.Width, canvas.Size.Height);
            canvas.Image = drawArea;
            currentColor = Color.Black;
            KeyPreview = true;
        }

        private int CheckNeighbourhood(Point p, double distance)
        {
            foreach (var pt in pointsList)
            {
                if (Math.Abs(pt.point.X - p.X) <= distance && Math.Abs(pt.point.Y - p.Y) <= distance)
                {
                    return pointsList.IndexOf(pt);
                }
            }
            return -1;
        }

        private int CheckedLine(int pt1, int pt2)
        {
            foreach (var ln in linesList)
            {
                if (ln.pt1 == pt1 && ln.pt2 == pt2 || ln.pt1 == pt2 && ln.pt2 == pt1)
                {
                    return linesList.IndexOf(ln);
                }
            }
            return -1;
        }

        private void canvas_MouseDown(object sender, MouseEventArgs e)
        {
            var pos = PointToClient(MousePosition);
            if (e.Button == MouseButtons.Left)
            {
                // update list of lines
                if(checkedEllipse)
                {
                    int pointIndex = CheckNeighbourhood(pos, RADIUS);
                    if(pointIndex != -1 && pointIndex != indexCheckedEllipse)
                    {
                        int lineIndex = CheckedLine(indexCheckedEllipse, pointIndex);
                        if (lineIndex == -1)
                        {
                            // add line
                            linesList.Add((indexCheckedEllipse, pointIndex));

                        }
                        else
                        {
                            // remove line
                            linesList.RemoveAt(lineIndex);
                        }
                    }
                }

                // update list of points
                if (CheckNeighbourhood(pos, neighborTolerant) == -1)
                {
                    pointsList.Add((pos, currentColor));
                }
                canvas.Refresh();
            }

            if (e.Button == MouseButtons.Right)
            {
                // update dashed ellipse
                int index = CheckNeighbourhood(pos, RADIUS); 
                if(index != -1)
                {
                    checkedEllipse = true;
                    indexCheckedEllipse = index;
                }
                else
                {
                    checkedEllipse = false;
                }
                canvas.Refresh();
            }

            if (e.Button == MouseButtons.Middle)
            {
                movingEllipse = true;
                canvas.Refresh();
            }
        }

        private void canvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle)
            {
                movingEllipse = false;
                canvas.Refresh();
            }
        }

        private void canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if(movingEllipse && checkedEllipse)
            {
                // update point
                pointsList[indexCheckedEllipse] = new(PointToClient(MousePosition),
                    pointsList[indexCheckedEllipse].color);
                canvas.Refresh();
            }
        }

        private void canvas_Paint(object sender, PaintEventArgs e)
        {
            buttonRemove.Enabled = checkedEllipse;

            // draw lines
            Pen blackPen = new Pen(Color.Black, 3);
            foreach (var ln in linesList)
            {
                e.Graphics.DrawLine(blackPen, pointsList[ln.pt1].point, pointsList[ln.pt2].point);
            }

            // draw ellipses
            int i = 1;
            foreach (var pt in pointsList)
            {
                var rect = new Rectangle(pt.point.X - RADIUS, pt.point.Y - RADIUS, RADIUS * 2, RADIUS * 2);
                e.Graphics.FillEllipse(new SolidBrush(canvas.BackColor), rect);
                e.Graphics.DrawEllipse(new Pen(pt.color, 3), rect);
                TextRenderer.DrawText(e.Graphics, (i).ToString(), this.Font, rect, pt.color,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                i++;
            }

            // draw dashed ellipse
            if (checkedEllipse)
            {
                var rect = new Rectangle(pointsList[indexCheckedEllipse].point.X - RADIUS,
                    pointsList[indexCheckedEllipse].point.Y - RADIUS, RADIUS * 2, RADIUS * 2);
                Pen dashedPen = new Pen(pointsList[indexCheckedEllipse].color, 3);
                dashedPen.DashPattern = new float[] { 2, 1 };
                e.Graphics.DrawEllipse(new Pen(canvas.BackColor, 3), rect);
                e.Graphics.DrawEllipse(dashedPen, rect);
                TextRenderer.DrawText(e.Graphics, (indexCheckedEllipse + 1).ToString(), this.Font, rect, pointsList[indexCheckedEllipse].color,
                 TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

        }

        // button click and delete

        private void buttonColor_Click(object sender, EventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog();
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                pictureBoxColor.BackColor = colorDialog.Color;
                currentColor = colorDialog.Color;
                if(checkedEllipse)
                {
                    pointsList[indexCheckedEllipse] = new(pointsList[indexCheckedEllipse].point, currentColor);
                }
                canvas.Refresh();
            }
        }

        // remove point and lines
        private void vertexRemove()
        {
            if (checkedEllipse)
            {
                linesList.RemoveAll(ln => pointsList[ln.pt1].point.Equals(pointsList[indexCheckedEllipse].point));
                linesList.RemoveAll(ln => pointsList[ln.pt2].point.Equals(pointsList[indexCheckedEllipse].point));
                pointsList.RemoveAt(indexCheckedEllipse);

                // update points indexes
                for(int i = 0; i < linesList.Count; i++)
                {
                    if(linesList[i].pt1 > indexCheckedEllipse)
                    {
                        linesList[i] = new (linesList[i].pt1 - 1, linesList[i].pt2);
                    }
                    if (linesList[i].pt2 > indexCheckedEllipse)
                    {
                        linesList[i] = new(linesList[i].pt1, linesList[i].pt2 - 1);
                    }
                }

                checkedEllipse = false;
                movingEllipse = false;
                canvas.Refresh();
            }
        }

        private void buttonRemove_Click(object sender, EventArgs e)
        {
            vertexRemove();
        }

        private void Form1_Delete(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                vertexRemove();
            }
        }

        private void buttonClear_Click(object sender, EventArgs e)
        {
            pointsList.Clear();
            linesList.Clear();
            checkedEllipse = false;
            movingEllipse = false;
            canvas.Refresh();
        }

        // https://stackoverflow.com/questions/21067507/change-language-at-runtime-in-c-sharp-winform

        private void applyResources(ComponentResourceManager resources, Control.ControlCollection ctls)
        {
            foreach (Control ctl in ctls)
            {
                resources.ApplyResources(ctl, ctl.Name);
                applyResources(resources, ctl.Controls);
            }
        }

        private void buttonPolish_Click(object sender, EventArgs e)
        {
            if (Thread.CurrentThread.CurrentUICulture.Name != "pl-PL")
            {
                var tmpSize = this.Size;
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("pl-PL");
                ComponentResourceManager resources = new ComponentResourceManager(typeof(Form1));
                resources.ApplyResources(this, "$this");
                applyResources(resources, this.Controls);
                this.Size = tmpSize;
            }
        }

        private void buttonEnglish_Click(object sender, EventArgs e)
        {
            if(Thread.CurrentThread.CurrentUICulture.Name != "en-GB")
            {
                var tmpSize = this.Size;
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-GB");
                ComponentResourceManager resources = new ComponentResourceManager(typeof(Form1));
                resources.ApplyResources(this, "$this");
                applyResources(resources, this.Controls);
                this.Size = tmpSize;
            }
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            Data data = new Data();
            data.pointsList = new();
            foreach(var pt in pointsList)
            {
                data.pointsList.Add((pt.point, new IntColor(pt.color.R, pt.color.G, pt.color.B)));
            }
            data.linesList = new(linesList);

            System.IO.Stream myStream;
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "graph files (*.graph)|*.graph";
            saveFileDialog.RestoreDirectory = true;

            if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                if ((myStream = saveFileDialog.OpenFile()) != null)
                {
                    try
                    {
                        XmlSerializer xs = new XmlSerializer(typeof(Data));
                        xs.Serialize(myStream, data);
                        myStream.Close();
                        MessageBox.Show("Graf zapisano pomyślnie!", "", MessageBoxButtons.OK);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Błąd serializacji!", "", MessageBoxButtons.OK);
                    }
                }
                else
                {
                    MessageBox.Show("Plik nie istnieje!", "", MessageBoxButtons.OK);
                }
            }
        }

        private void buttonOpen_Click(object sender, EventArgs e)
        {
            System.IO.Stream myStream;
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "graph files (*.graph)|*.graph";
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                if ((myStream = openFileDialog.OpenFile()) != null)
                {
                    try
                    {
                        XmlSerializer xs = new XmlSerializer(typeof(Data));
                        Data data = (Data)xs.Deserialize(myStream);
                        myStream.Close();
                        pointsList = new();
                        foreach (var pt in data.pointsList)
                        {
                            pointsList.Add((pt.point, Color.FromArgb(pt.intColor.r, pt.intColor.g, pt.intColor.b)));
                        }
                        linesList = new(data.linesList);
                        checkedEllipse = false;
                        canvas.Refresh();
                        MessageBox.Show("Graf wczytano pomyślnie!", "", MessageBoxButtons.OK);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Błąd deserializacji!", "", MessageBoxButtons.OK);
                    }
                }
                else
                {
                    MessageBox.Show("Plik nie istnieje!", "", MessageBoxButtons.OK);
                }
            }
        }

        // classes to serialize

        [Serializable] public class Data
        {
            public List<(Point point, IntColor intColor)> pointsList = new();
            public List<(int pt1, int pt2)> linesList = new();
        }

        [Serializable] public class IntColor
        {
            public int r, g, b;

            public IntColor()
            {
                r = g = b = 0;
            }

            public IntColor(int r, int g, int b)
            {
                this.r = r;
                this.g = g;
                this.b = b;
            }
        }
    }
}
