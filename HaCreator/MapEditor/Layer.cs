﻿/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using MapleLib.WzLib;
using MapleLib.WzLib.WzProperties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaCreator.MapEditor
{
    //the difference between LayeredItem and this is that LayeredItems are actually 
    //ordered according to their layer (tiles\objs) in the editor. IContainsLayerInfo only
    //contains info about layers, and is not necessarily drawn according to it.
    public interface IContainsLayerInfo
    {
        int LayerNumber { get; set; }
    }

    public class Layer
    {
        private List<LayeredItem> items = new List<LayeredItem>(); //needed?
        private int num;
        private Board board;
        private string _tS = null;

        public Layer(Board board)
        {
            this.board = board;
            if (board.Layers.Count == 10) throw new NotSupportedException("Cannot add more than 10 layers (why would you need that much anyway?)");
            num = board.Layers.Count;
            board.Layers.Add(this);
        }

        public List<LayeredItem> Items
        {
            get
            {
                return items;
            }
            set
            {
                items = value;
            }
        }

        public int LayerNumber
        {
            get
            {
                return num;
            }
        }

        public string tS
        {
            get { return _tS; }
            set 
            {
                lock (board.ParentControl)
                {
                    if (_tS != value)
                    {
                        _tS = value;
                        board.ParentControl.LayerTSChanged(this);
                    }
                }
            }
        }

        public void ReplaceTS(string newTS)
        {
            lock (board.ParentControl)
            {
                foreach (LayeredItem item in items)
                {
                    if (item is TileInstance)
                    {
                        TileInstance tile = (TileInstance)item;
                        TileInfo tileBase = (TileInfo)tile.BaseInfo;
                        IWzImageProperty tCat = Program.InfoManager.TileSets[newTS][tileBase.u];
                        IWzImageProperty tProp = tCat[tileBase.no];
                        if (tProp == null)
                        {
                            tProp = tCat["0"];
                        }
                        if (tProp.HCTag == null)
                            tProp.HCTag = TileInfo.Load((WzCanvasProperty)tProp, newTS, tileBase.u, tileBase.no);
                        TileInfo tileInfo = (TileInfo)tProp.HCTag;
                        tile.SetBaseInfo(tileInfo);
                    }
                }
            }
            this.tS = newTS;
        }

        public static Layer GetLayerByNum(Board board, int num)
        {
            return board.Layers[num];
        }

        public void RecheckTileSet()
        {
            foreach (LayeredItem item in items)
                if (item is TileInstance)
                {
                    tS = ((TileInfo)item.BaseInfo).tS;
                    return;
                }
            tS = null;
        }
    }

    public abstract class LayeredItem : BoardItem, IContainsLayerInfo
    {
        private Layer layer;

        public LayeredItem(Board board, Layer layer, int x, int y, int z, bool beforeAdding)
            : base(board, x, y, z, beforeAdding)
        {
            this.layer = layer;
            layer.Items.Add(this);
        }

        public override void RemoveItem(ref List<UndoRedoAction> undoPipe)
        {
            lock (board.ParentControl)
            {
                layer.Items.Remove(this);
                base.RemoveItem(ref undoPipe);
            }
        }

        public override void InsertItem()
        {
            lock (board.ParentControl)
            {
                layer.Items.Add(this);
                base.InsertItem();
            }
        }

        public Layer Layer
        {
            get
            {
                return layer;
            }
            set
            {
                lock (board.ParentControl)
                {
                    layer.Items.Remove(this);
                    layer = value;
                    layer.Items.Add(this);
                    Board.BoardItems.Sort();
                }
            }
        }

        public int LayerNumber
        {
            get { return Layer.LayerNumber; }
            set
            {
                lock (board.ParentControl)
                {
                    Layer = Board.Layers[value];
                }
            }
        }
    }
}
