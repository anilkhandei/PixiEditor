﻿using PixiEditor.Helpers;
using PixiEditor.Models.Enums;
using PixiEditor.Models.Layers;
using PixiEditor.Models.Position;
using PixiEditor.Models.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PixiEditor.Models.Controllers
{
    public class BitmapOperationsUtility
    {

        private Coordinates _lastMousePos;
        private BitmapPixelChanges _lastChangedPixels;

        public event EventHandler<BitmapChangedEventArgs> BitmapChanged;

        public BitmapManager Manager { get; set; }

        public BitmapOperationsUtility(BitmapManager manager)
        {
            Manager = manager;
        }

        public void ExecuteTool(Coordinates newPos, List<Coordinates> mouseMove, BitmapOperationTool tool)
        {
            if (tool != null && tool.ToolType != ToolType.None)
            {
                if (Manager.Layers.Count == 0 || mouseMove.Count == 0) return;
                mouseMove.Reverse();
                UseTool(mouseMove, tool, Manager.PrimaryColor);

                _lastMousePos = newPos;
            }
        }

        public void StopAction()
        {
            BitmapPixelChanges oldValues = GetOldPixelsValues(_lastChangedPixels.ChangedPixels.Keys.ToArray());
            Manager.ActiveLayer.ApplyPixels(_lastChangedPixels);
            BitmapChanged?.Invoke(this, new BitmapChangedEventArgs(_lastChangedPixels, oldValues, Manager.ActiveLayerIndex));
            Manager.PreviewLayer.Clear();
        }

      

        private void UseTool(List<Coordinates> mouseMoveCords, BitmapOperationTool tool, Color color)
        {
            if (Keyboard.IsKeyDown(Key.LeftShift) && !MouseCordsNotInLine(mouseMoveCords))
            {
                mouseMoveCords = GetSquareCoordiantes(mouseMoveCords);
            };
                if (!tool.RequiresPreviewLayer)
                {
                    BitmapPixelChanges changedPixels = tool.Use(Manager.ActiveLayer, mouseMoveCords.ToArray(), color);
                    BitmapPixelChanges oldPixelsValues = GetOldPixelsValues(changedPixels.ChangedPixels.Keys.ToArray());
                    Manager.ActiveLayer.ApplyPixels(changedPixels);
                    BitmapChanged?.Invoke(this, new BitmapChangedEventArgs(changedPixels, oldPixelsValues, Manager.ActiveLayerIndex));
                }
                else
                {
                    UseToolOnPreviewLayer(mouseMoveCords);
                }           
        }

        private bool MouseCordsNotInLine(List<Coordinates> cords)
        {
            return cords[0].X == cords[^1].X || cords[0].Y == cords[^1].Y;
        }

        /// <summary>
        /// Extracts square from rectangle mouse drag, used to draw symmetric shapes.
        /// </summary>
        /// <param name="mouseMoveCords"></param>
        /// <returns></returns>
        private List<Coordinates> GetSquareCoordiantes(List<Coordinates> mouseMoveCords)
        {
            int xLength = mouseMoveCords[0].Y - mouseMoveCords[^1].Y;
            int yLength = mouseMoveCords[0].Y - mouseMoveCords[^1].Y;
            if(mouseMoveCords[^1].Y > mouseMoveCords[0].Y)
            {
                xLength *= -1;
            }
            if(mouseMoveCords[^1].X > mouseMoveCords[0].X)
            {
                xLength *= -1;
            }
            mouseMoveCords[0] = new Coordinates(mouseMoveCords[^1].X + xLength, mouseMoveCords[^1].Y + yLength);
            return mouseMoveCords;
        }

        private BitmapPixelChanges GetOldPixelsValues(Coordinates[] coordinates)
        {
            Dictionary<Coordinates, Color> values = new Dictionary<Coordinates, Color>();
            Manager.ActiveLayer.LayerBitmap.Lock();
            for (int i = 0; i < coordinates.Length; i++)
            {
                if (coordinates[i].X < 0 || coordinates[i].X > Manager.Layers[0].Width - 1 || coordinates[i].Y < 0 || coordinates[i].Y > Manager.Layers[0].Height - 1) 
                    continue;
                values.Add(coordinates[i], Manager.ActiveLayer.LayerBitmap.GetPixel(coordinates[i].X, coordinates[i].Y));
            }
            Manager.ActiveLayer.LayerBitmap.Unlock();
            return new BitmapPixelChanges(values);
        }

        private void UseToolOnPreviewLayer(List<Coordinates> mouseMove)
        {
            BitmapPixelChanges changedPixels;
            if (mouseMove.Count > 0 && mouseMove[0] != _lastMousePos)
            {
                Manager.GeneratePreviewLayer();
                Manager.PreviewLayer.Clear();
                changedPixels = ((BitmapOperationTool)Manager.SelectedTool).Use(Manager.ActiveLayer, mouseMove.ToArray(), Manager.PrimaryColor);
                Manager.PreviewLayer.ApplyPixels(changedPixels);
                _lastChangedPixels = changedPixels;
            }
        }      
    }
}

public class BitmapChangedEventArgs : EventArgs
{
    public BitmapPixelChanges PixelsChanged { get; set; }
    public BitmapPixelChanges OldPixelsValues { get; set; }
    public int ChangedLayerIndex { get; set; }

    public BitmapChangedEventArgs(BitmapPixelChanges pixelsChanged, BitmapPixelChanges oldPixelsValues, int changedLayerIndex)
    {
        PixelsChanged = pixelsChanged;
        OldPixelsValues = oldPixelsValues;
        ChangedLayerIndex = changedLayerIndex;
    }
}