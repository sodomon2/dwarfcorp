// GatherTool.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp
{
    /// <summary>
    /// When using this tool, the player specifies that certain
    /// entities should be put into stockpiles.
    /// </summary>
    public class GatherTool : PlayerTool
    {
        public GatherTool()
        {

        }

        public override void OnBegin()
        {

        }

        public override void OnEnd()
        {

        }


        public bool CanGather(GameComponent c)
        {
            return c.Tags.Contains("Resource") &&
                c.Active &&
                c.IsVisible &&
                c.Parent == Player.World.ComponentManager.RootComponent;
        }

        public override void OnBodiesSelected(List<GameComponent> bodies, InputManager.MouseButton button)
        {
            List<Task> assignments = new List<Task>();

            foreach (var resource in bodies.Where(body => CanGather(body)))
            {
                if (Player.World.ChunkManager.IsAboveCullPlane(resource.BoundingBox)) continue;

                if (button == InputManager.MouseButton.Left)
                {
                    assignments.Add(new GatherItemTask(resource));
                }
                else
                {
                    var designation = Player.Faction.Designations.GetEntityDesignation(resource, DesignationType.Gather);
                    if (designation != null)
                        Player.TaskManager.CancelTask(designation.Task);
                }
            }

            Player.TaskManager.AddTasks(assignments);

            OnConfirm(Faction.FilterMinionsWithCapability(Player.World.Master.SelectedMinions, Task.TaskCategory.Gather));
        }

        public override void OnMouseOver(IEnumerable<GameComponent> bodies)
        {
            DefaultOnMouseOver(bodies);
        }

        public override void OnVoxelsSelected(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {

        }

        public override void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {

        }

        public override void Update(DwarfGame game, DwarfTime time)
        {
            if (Player.IsCameraRotationModeActive())
                return;

            Player.VoxSelector.Enabled = false;
            Player.BodySelector.Enabled = true;
            Player.BodySelector.AllowRightClickSelection = true;

            if (Player.World.IsMouseOverGui)
                Player.World.SetMouse(Player.World.MousePointer);
            else
                Player.World.SetMouse(new Gui.MousePointer("mouse", 1, 6));
        }

        public override void Render3D(DwarfGame game, DwarfTime time)
        {
            NamedImageFrame frame = new NamedImageFrame("newgui/pointers", 32, 6, 0);

            foreach (var body in Player.BodySelector.CurrentBodies)
            {
                if (body.Tags.Contains("Resource"))
                {
                    Drawer2D.DrawText(body.Name, body.Position, Color.White, Color.Black);
                    BoundingBox bounds = body.BoundingBox;
                    Drawer3D.DrawBox(bounds, Color.Orange, 0.02f, false);
                    Drawer2D.DrawSprite(frame, body.Position + Vector3.One * 0.5f, Vector2.One * 0.5f, Vector2.Zero, new Color(255, 255, 255, 100));
                }
            }
        }

        public override void Render2D(DwarfGame game, DwarfTime time)
        {
        }

    }
}