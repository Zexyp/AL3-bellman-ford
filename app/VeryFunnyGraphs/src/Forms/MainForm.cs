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
        readonly Size NODE_SIZE = new Size(32, 32);

        readonly Pen CONNECTION_ARROW_PEN;
        readonly Pen CONNECTION_PEN = new Pen(Color.FromArgb(50, 50, 50), 2);

        const int GRID_SPACING = 64;
        readonly Pen GRID_PEN = new Pen(Color.FromArgb(32, 128, 128, 128), 1);

        // editor context
        private Point mouseDownLocation;
        private Button movingNode;
        private Point mouseLocation;
        private Point prevMouseMove;
        private Point gridOffset;
        ToolTip[] toolTips = null;

        Mode mode;
        Preferences preferences;

        // data
        GraphContainer<Button> graph = new GraphContainer<Button>();

        public MainForm()
        {
            // constants
            CONNECTION_ARROW_PEN = (Pen)CONNECTION_PEN.Clone();
            CONNECTION_ARROW_PEN.CustomEndCap = new AdjustableArrowCap(8, 8);

            // preferences
            preferences.host = "localhost";
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

            OnResize(null);
        }

        private static bool LineaIntersectio(PointF point, PointF l1, PointF l2)
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

        public static void SetDoubleBuffered(Control c)
        {
            PropertyInfo prop = typeof(Control).GetProperty("DoubleBuffered", BindingFlags.NonPublic | BindingFlags.Instance);
            prop.SetValue(c, true);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            viewPanel.Size = this.ClientSize - new Size(viewPanel.Location);
            nodeListBox.Location = new Point(viewPanel.ClientSize.Width - nodeListBox.Width, editorMenuStrip.Bottom);
            nodeListBox.Height = viewPanel.ClientSize.Height;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (ViewPanel_KeyInput(keyData))
                return true;

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void AddNode(Button node)
        {
            ClearResult();

            node.Click += Node_Click;
            node.MouseMove += NodeMove_MouseMove;
            node.MouseDown += NodeMove_MouseDown;
            node.MouseUp += NodeMove_MouseUp;
            node.PreviewKeyDown += Node_PreviewKeyDown;
            node.Paint += Node_Paint;
            viewPanel.Controls.Add(node);
            graph.AddVertex(node);

            nodeListBox.Items.Add(node);

            viewPanel.Invalidate();

            RecalcNodes();
        }

        private void RemoveNode(Button node)
        {
            ClearResult();

            node.Click -= Node_Click;
            node.MouseMove -= NodeMove_MouseMove;
            node.MouseDown -= NodeMove_MouseDown;
            node.MouseUp -= NodeMove_MouseUp;
            node.PreviewKeyDown -= Node_PreviewKeyDown;
            node.Paint -= Node_Paint;
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

        private void ConnectNodes(Button a, Button b)
        {
            ClearResult();

            if (graph.ContainsEdge(a, b))
                graph.Disconnect(a, b);
            graph.Connect(a, b);
        }

        private void MoveView(Size delta)
        {
            for (int i = 0; i < graph.Vertices.Count; i++)
            {
                graph.Vertices[i].Location += delta;
            }

            gridOffset += delta;
            gridOffset = new Point(gridOffset.X % GRID_SPACING, gridOffset.Y % GRID_SPACING);

            viewPanel.Invalidate();
            viewPanel.Update();
        }

        private void ClearResult()
        {
            if (toolTips == null)
                return;

            for (int i = 0; i < toolTips.Length; i++)
            {
                toolTips[i]?.Dispose();
                toolTips[i] = null;
                    
            }
            toolTips = null;

            for (int i = 0; i < graph.Vertices.Count; i++)
            {
                if (graph.Vertices[i] != graph.Start)
                    graph.Vertices[i].BackColor = BackColor;
            }
        }

        #region Node List
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
        #endregion

        #region View Panel
        private bool ViewPanel_KeyInput(Keys keyData)
        {
            if (keyData == Keys.Enter)
            {
                var dump = graph.Dump();
                Clipboard.SetText(dump);
                MessageBox.Show(dump);
                return true;
            }

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

            return false;
        }

        private void ViewPanel_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            for (int y = 0; y < viewPanel.Height; y += GRID_SPACING)
            {
                g.DrawLine(GRID_PEN, new Point(0, y + gridOffset.Y), new Point(viewPanel.Width, y + gridOffset.Y));
            }
            for (int x = 0; x < viewPanel.Width; x += GRID_SPACING)
            {
                g.DrawLine(GRID_PEN, new Point(x + gridOffset.X, 0), new Point(x + gridOffset.X, viewPanel.Height));
            }

            using var ellipseBrush = new SolidBrush(BackColor);
            for (int i = 0; i < graph.Edges.Count; i++)
            {
                var edge = graph.Edges[i];
                var edgeStart = edge.A.Location + NODE_SIZE / 2;
                var edgeEnd = edge.B.Location + NODE_SIZE / 2;

                //Vector2 vecArrowEnd = new Vector2(edgeEnd.X - edgeStart.X, edgeEnd.Y - edgeStart.Y);
                //vecArrowEnd = Vector2.Normalize(vecArrowEnd) * 64;
                //var edgeArrowEnd = new PointF(vecArrowEnd.X + edgeStart.X, vecArrowEnd.Y + edgeStart.Y);

                var edgeDelta = new Point(edgeEnd.X - edgeStart.X, edgeEnd.Y - edgeStart.Y);
                var edgeArrowEnd = new Point(edgeDelta.X / 3 + edgeStart.X, edgeDelta.Y / 3 + edgeStart.Y);

                g.DrawLine(CONNECTION_ARROW_PEN, edgeStart, edgeArrowEnd);
                g.DrawLine(CONNECTION_PEN, edgeArrowEnd, edgeEnd);

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
                g.DrawLine(CONNECTION_PEN, movingNode.Location + NODE_SIZE / 2, mouseLocation);
            }
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

                    ClearResult();
                    break;
                }
            }

            viewPanel.Invalidate();
        }

        private void ViewPanel_Click(object sender, EventArgs e)
        {
            ActiveControl = viewPanel;
            nodeListBox.ClearSelected();
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
        #endregion

        #region Node
        private void Node_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var button = ((Button)sender);
            using var backBrush = new SolidBrush(button.BackColor);
            using var foreBrush = new SolidBrush(button.ForeColor);
            var border = 4;
            g.FillRectangle(backBrush, new Rectangle(new Point(border, border), new Size(button.Width - border * 2, button.Height - border * 2)));
            var measure = g.MeasureString(button.Text, Font);
            var startPoint = new PointF(button.Width / 2, button.Height / 2) - new SizeF(measure.Width / 2, measure.Height / 2);
            g.DrawString(button.Text, Font, foreBrush, startPoint);
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

                    ConnectNodes(tmp, leftOn);

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
        #endregion
        
        #region Editor Tool Strip
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

        private void markStartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!graph.Vertices.Contains(ActiveControl))
            {
                MessageBox.Show("No node selected.");
                return;
            }

            ClearResult();

            for (int i = 0; i < graph.Vertices.Count; i++)
            {
                graph.Vertices[i].BackColor = BackColor;
            }

            ActiveControl.BackColor = Color.FromArgb(0, 224, 0);

            graph.Start = (Button)ActiveControl;
        }
        #endregion

        #region Main Tool Strip
        private void preferencesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PreferencesForm form = new PreferencesForm();
            if (form.Edit(preferences, out var result))
                preferences = result;
        }

        private void solveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (graph.Start == null)
            {
                MessageBox.Show("No start node.");
                return;
            }

            ClearResult();

            string response;
            try
            {
                Connector connector = new Connector();
                response = connector.Use(preferences.host, preferences.port, graph.Dump());
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                MessageBox.Show("Attmept to process data failed.");
                return;
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(response);
                var root = document.RootElement;
                var vertices = root.GetProperty("vertices");
                toolTips = new ToolTip[vertices.GetArrayLength()];
                int i = 0;
                int step = 32;
                foreach (var item in vertices.EnumerateArray())
                {
                    int index = item.GetProperty("id").GetInt32();
                    int distance = item.GetProperty("distance").GetInt32();
                    bool cycle = item.GetProperty("isInNegativeLoop").GetBoolean();

                    if (graph.Vertices[index] == graph.Start) continue;

                    graph.Vertices[index].BackColor = Color.FromArgb(Math.Clamp(distance * step, 127, 255), 191, Math.Clamp(distance * -step, 127, 255));
                    if (cycle)
                        graph.Vertices[index].BackColor = Color.FromArgb(255, 58, 58);

                    toolTips[i] = new ToolTip();
                    toolTips[i].SetToolTip(graph.Vertices[index], $"Distance: {(cycle ? "-∞" : distance)}");
                    i++;
                }
            }
            catch (KeyNotFoundException ex)
            {
                Debug.WriteLine(ex);
                MessageBox.Show("Processing results failed.");
                return;
            }
        }
        #endregion
    }

    public struct Preferences
    {
        public string host;
        public int port;
    }
}
