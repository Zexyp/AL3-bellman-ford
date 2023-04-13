using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Reflection;
using System.Numerics;
using System.Text.Json;

namespace VeryFunnyGraphs.Forms
{
    public partial class MainForm : Form
    {
        enum Mode
        {
            Move,
            Connect
        }

        // visuals
        readonly Size NODE_SIZE = new Size(40, 40);
        readonly Pen PEN_LINES = new Pen(Brushes.DarkGray, 2);

        // editor context
        private Point mouseDownLocation;
        private Button movingNode;
        private Point mouseLocation;
        private Point prevMouseMove;

        Mode mode;
        Preferences preferences;

        // data
        GraphContainer<Button> graph = new GraphContainer<Button>();

        public MainForm()
        {
            preferences.host = "169.254.51.103";
            preferences.port = 6969;

            InitializeComponent();

            viewPanel.Paint += ViewPanel_Paint;
            viewPanel.MouseMove += ViewPanel_MouseMove;
            viewPanel.MouseDown += ViewPanel_MouseDown;
            viewPanel.Click += ViewPanel_Click;
            viewPanel.DoubleClick += ViewPanel_DoubleClick;
            viewPanel.MouseWheel += ViewPanel_MouseWheel;
            SetDoubleBuffered(viewPanel);

            nodeListBox.DrawMode = DrawMode.OwnerDrawFixed;
            nodeListBox.DrawItem += NodeListBox_DrawItem;
            nodeListBox.SelectedValueChanged += NodeListBox_SelectedValueChanged;
            nodeListBox.DoubleClick += NodeListBox_DoubleClick;

            moveToolStripMenuItem_Click(null, null);
        }

        private void ViewPanel_DoubleClick(object sender, EventArgs e)
        {
            for (int i = 0; i < graph.Edges.Count; i++)
            {
                var edge = graph.Edges[i];
                if (LineaIntersectio(viewPanel.PointToClient(MousePosition), edge.A.Location + NODE_SIZE / 2, edge.B.Location + NODE_SIZE / 2))
                {
                    graph.Disconnect(edge.A, edge.B);
                    viewPanel.Invalidate();
                    break;
                }
            }
        }

        private void ViewPanel_MouseWheel(object sender, MouseEventArgs e)
        {
            if (ModifierKeys != Keys.Control)
                return;

            for (int i = 0; i < graph.Edges.Count; i++)
            {
                var edge = graph.Edges[i];
                if (LineaIntersectio(viewPanel.PointToClient(MousePosition), edge.A.Location + NODE_SIZE / 2, edge.B.Location + NODE_SIZE / 2))
                {
                    int delta = 0;
                    if (e.Delta > 0)
                        delta = 1;
                    if (e.Delta < 0)
                        delta = -1;
                    edge.Weight += delta;
                    break;
                }
            }

            viewPanel.Invalidate();
        }

        private bool LineaIntersectio(PointF point, PointF l1, PointF l2)
        {
            float minimum_distance(Vector2 v, Vector2 w, Vector2 p)
            {
                // Return minimum distance between line segment vw and point p
                float l2 = (v - w).LengthSquared();  // i.e. |w-v|^2 -  avoid a sqrt
                if (l2 == 0.0) return (p - v).Length();   // v == w case
                                                        // Consider the line extending the segment, parameterized as v + t (w - v).
                                                        // We find projection of point p onto the line. 
                                                        // It falls where t = [(p-v) . (w-v)] / |w-v|^2
                                                        // We clamp t from [0,1] to handle points outside the segment vw.
                float t = Math.Max(0, Math.Min(1, Vector2.Dot(p - v, w - v) / l2));
                Vector2 projection = v + t * (w - v);  // Projection falls on the segment
                return (p - projection).Length();
            }

            return minimum_distance(new Vector2(l1.X, l1.Y), new Vector2(l2.X, l2.Y), new Vector2(point.X, point.Y)) <= 8;
        }

        private void ViewPanel_Click(object sender, EventArgs e)
        {
            ActiveControl = viewPanel;
            nodeListBox.ClearSelected();
        }

        private void NodeListBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0)
                return;

            e.DrawBackground();

            using Brush brush = new SolidBrush(e.ForeColor);
            e.Graphics.DrawString(graph.Vertices.IndexOf((Button)nodeListBox.Items[e.Index]).ToString(), e.Font, brush, e.Bounds);

            e.DrawFocusRectangle();
        }

        private void NodeListBox_SelectedValueChanged(object sender, EventArgs e)
        {
            if (nodeListBox.SelectedItem == null)
                return;

            ((Button)nodeListBox.SelectedItem).Focus();
        }

        private void NodeListBox_DoubleClick(object sender, EventArgs e)
        {
            if (nodeListBox.SelectedItem == null)
                return;

            var btn = ((Button)nodeListBox.SelectedItem);
            var delta = new Size(-btn.Location.X, -btn.Location.Y) + viewPanel.ClientSize / 2;
            MoveView(delta);
            NodeListBox_SelectedValueChanged(sender, e);
        }

        public static void SetDoubleBuffered(Control c)
        {
            PropertyInfo prop = typeof(Control).GetProperty("DoubleBuffered", BindingFlags.NonPublic | BindingFlags.Instance);
            prop.SetValue(c, true);
        }

        private void ViewPanel_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using var ellipseBrush = new SolidBrush(BackColor);
            for (int i = 0; i < graph.Edges.Count; i++)
            {
                var edge = graph.Edges[i];
                g.DrawLine(PEN_LINES, edge.A.Location + NODE_SIZE / 2, edge.B.Location + NODE_SIZE / 2);

                var mid = new Point(new Size(edge.A.Location + new Size(edge.B.Location)) / 2);
                string title = edge.Weight.ToString();
                var titleSize = g.MeasureString(title, Font);
                var ellipseSize = new SizeF(Math.Max(titleSize.Width, titleSize.Height), Math.Max(titleSize.Width, titleSize.Height));
                var titlePos = new PointF(mid.X, mid.Y) - titleSize / 2 + NODE_SIZE / 2;
                var ellipsePos = new PointF(mid.X, mid.Y) - ellipseSize / 2 + NODE_SIZE / 2;

                g.FillEllipse(ellipseBrush, new RectangleF(ellipsePos, ellipseSize));

                g.DrawString(title, Font, Brushes.Black, titlePos);
            }

            if (mode == Mode.Connect && movingNode != null)
            {
                g.DrawLine(PEN_LINES, movingNode.Location + NODE_SIZE / 2, mouseLocation);
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Enter)
                MessageBox.Show(graph.Dump());

            int moveDist = 32;

            if (ModifierKeys == Keys.Control)
                switch (keyData ^ Keys.Control)
                {
                    case Keys.Left:
                        MoveView(new Size(moveDist, 0));
                        return true;
                    case Keys.Right:
                        MoveView(new Size(-moveDist, 0));
                        return true;
                    case Keys.Up:
                        MoveView(new Size(0, moveDist));
                        return true;
                    case Keys.Down:
                        MoveView(new Size(0, -moveDist));
                        return true;
                }
            else if (graph.Vertices.Contains(ActiveControl))
                switch (keyData)
                {
                    case Keys.Left:
                        ActiveControl.Location += new Size(-moveDist, 0);
                        viewPanel.Invalidate();
                        return true;
                    case Keys.Right:
                        ActiveControl.Location += new Size(moveDist, 0);
                        viewPanel.Invalidate();
                        return true;
                    case Keys.Up:
                        ActiveControl.Location += new Size(0, -moveDist);
                        viewPanel.Invalidate();
                        return true;
                    case Keys.Down:
                        ActiveControl.Location += new Size(0, moveDist);
                        viewPanel.Invalidate();
                        return true;
                }

            if (keyData == Keys.Tab)
            {
                if (connectToolStripMenuItem.Checked)
                {
                    moveToolStripMenuItem_Click(null, null);
                    return true;
                }
                if (moveToolStripMenuItem.Checked)
                {
                    connectToolStripMenuItem_Click(null, null);
                    return true;
                }
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void ViewPanel_MouseDown(object sender, MouseEventArgs e)
        {
            prevMouseMove = new Point(e.X, e.Y);

            if (ModifierKeys.HasFlag(Keys.Control))
            {
                var funny = new Button();
                funny.Location = e.Location - NODE_SIZE / 2;
                funny.Size = NODE_SIZE;
                AddNode(funny);
            }
        }

        private void ViewPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Middle)
                return;

            var delta = new Size(e.X, e.Y) - new Size(prevMouseMove);
            prevMouseMove = new Point(e.X, e.Y);

            MoveView(delta);
        }

        private void NodeMove_MouseUp(object sender, MouseEventArgs e)
        {
            switch (mode)
            {
                case Mode.Move:
                    movingNode = null;
                    break;
                case Mode.Connect:
                    var tmp = movingNode;
                    movingNode = null;

                    viewPanel.Invalidate();

                    var leftOn = viewPanel.GetChildAtPoint(((Button)sender).Location + new Size(e.Location), GetChildAtPointSkip.None) as Button;

                    if (tmp == leftOn || tmp == null || leftOn == null) break;

                    if (!graph.ContainsEdge(tmp, leftOn))
                        graph.Connect(tmp, leftOn);

                    break;
                default:
                    Debug.Assert(false);
                    break;
            }
        }

        private void NodeMove_MouseMove(object sender, MouseEventArgs e)
        {
            switch (mode)
            {
                case Mode.Move:
                    if (movingNode != null)
                    {
                        movingNode.Location = new Point(
                            e.X + movingNode.Left - mouseDownLocation.X,
                            e.Y + movingNode.Top - mouseDownLocation.Y);

                        viewPanel.Invalidate();
                        viewPanel.Update();
                    }
                    break;
                case Mode.Connect:
                    if (movingNode != null)
                    {
                        mouseLocation.X = e.X + movingNode.Left;
                        mouseLocation.Y = e.Y + movingNode.Top;

                        viewPanel.Invalidate();
                    }
                    break;
                default:
                    Debug.Assert(false);
                    break;
            }
        }

        private void NodeMove_MouseDown(object sender, MouseEventArgs e)
        {
            switch (mode)
            {
                case Mode.Move:
                    movingNode = (Button)sender;
                    mouseDownLocation = e.Location;
                    break;
                case Mode.Connect:
                    movingNode = (Button)sender;
                    mouseLocation = e.Location;
                    break;
                default:
                    Debug.Assert(false);
                    break;
            }
        }

        private void Node_Click(object sender, EventArgs e)
        {
            nodeListBox.SetSelected(nodeListBox.Items.IndexOf(sender), true);
        }

        private void Node_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            switch (e.KeyData)
            {
                case Keys.X:
                    RemoveNode((Button)sender);
                    break;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            viewPanel.Size = this.ClientSize - new Size(viewPanel.Location);
            nodeListBox.Location = new Point(viewPanel.ClientSize.Width - nodeListBox.Width, editorMenuStrip.Bottom);
            nodeListBox.Height = viewPanel.ClientSize.Height;
        }

        private void AddNode(Button node)
        {
            node.Click += Node_Click;
            node.MouseMove += NodeMove_MouseMove;
            node.MouseDown += NodeMove_MouseDown;
            node.MouseUp += NodeMove_MouseUp;
            node.PreviewKeyDown += Node_PreviewKeyDown;
            viewPanel.Controls.Add(node);
            graph.AddVertex(node);

            nodeListBox.Items.Add(node);

            viewPanel.Invalidate();

            RecalcNodes();
        }

        private void RemoveNode(Button node)
        {
            node.Click -= Node_Click;
            node.MouseMove -= NodeMove_MouseMove;
            node.MouseDown -= NodeMove_MouseDown;
            node.MouseUp -= NodeMove_MouseUp;
            node.PreviewKeyDown += Node_PreviewKeyDown;
            viewPanel.Controls.Remove(node);
            graph.RemoveVertex(node);

            nodeListBox.Items.Remove(node);

            node.Dispose();

            viewPanel.Invalidate();

            RecalcNodes();
        }

        private void RecalcNodes()
        {
            for (int i = 0; i < graph.Vertices.Count; i++)
            {
                graph.Vertices[i].Text = i.ToString();
            }
        }

        private void MoveView(Size delta)
        {
            for (int i = 0; i < graph.Vertices.Count; i++)
            {
                graph.Vertices[i].Location += delta;
            }

            viewPanel.Invalidate();
            viewPanel.Update();
        }

        private void moveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mode = Mode.Move;

            moveToolStripMenuItem.Checked = true;

            connectToolStripMenuItem.Checked = false;
        }

        private void connectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mode = Mode.Connect;

            connectToolStripMenuItem.Checked = true;

            moveToolStripMenuItem.Checked = false;
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            while (graph.Vertices.Count > 0)
                RemoveNode(graph.Vertices[0]);
        }

        private void preferencesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PreferencesForm form = new PreferencesForm();
            if (form.Edit(preferences, out var result))
                preferences = result;
        }

        private void markStartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!graph.Vertices.Contains(ActiveControl))
            {
                MessageBox.Show("No node selected.");
                return;
            }

            for (int i = 0; i < graph.Vertices.Count; i++)
            {
                graph.Vertices[i].UseVisualStyleBackColor = true;
            }

            ActiveControl.BackColor = Color.LightGreen;

            graph.Start = (Button)ActiveControl;
        }

        private void solveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (graph.Start == null)
            {
                MessageBox.Show("No start node.");
                return;
            }

            string response;
            try
            {
                Connector connector = new Connector();
                response = connector.Use(preferences.host, preferences.port, graph.Dump());
            }
            catch (Exception)
            {
                MessageBox.Show("Attmept to process data failed.");
                return;
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(response);
                var root = document.RootElement;
            }
            catch (KeyNotFoundException)
            {
                MessageBox.Show("Processing results failed.");
                return;
            }
        }
    }

    public struct Preferences
    {
        public string host;
        public int port;
    }
}
