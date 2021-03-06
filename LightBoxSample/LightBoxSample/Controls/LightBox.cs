﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace LightBoxSample.Controls
{
    class LightBox
    {
        public static void Show(UIElement owner, FrameworkElement content)
        {
            var adorner = GetAdorner(owner);
            adorner.AddDialog(content);
        }

        private static LightBoxAdorner GetAdorner(UIElement element)
        {
            // Window系のクラスだったら、Contentプロパティを利用。それ以外の場合はそのまま利用。
            var target = (element as Window)?.Content as UIElement ?? element;

            if (element == null) return null;
            var layer = AdornerLayer.GetAdornerLayer(target);
            if (layer == null) return null;

            var current = layer.GetAdorners(target)
                               ?.OfType<LightBoxAdorner>()
                               ?.SingleOrDefault();

            if (current != null)
            {
                return current;
            }
            else
            {
                // ダイアログ用のAdornerが存在してないので、新規に作って設定して返す。
                var adorner = new LightBoxAdorner(target, element);

                // すべてのダイアログがクリアされたときに、Adornerを削除するための処理を追加
                adorner.AllDialogClosed += (s, e) => { ClearAdorner(layer, adorner); };
                layer.Add(adorner);
                return adorner;
            }
        }


        private static void ClearAdorner(AdornerLayer layer, LightBoxAdorner adorner)
        {
            // null条件演算子でいいかも。
            if (layer != null && adorner != null)
            {
                layer.Remove(adorner);
            }
        }


        #region 色々と添付プロパティの定義

        public static ControlTemplate GetTemplate(DependencyObject obj)
        {
            return (ControlTemplate)obj.GetValue(TemplateProperty);
        }
        public static void SetTemplate(DependencyObject obj, ControlTemplate value)
        {
            obj.SetValue(TemplateProperty, value);
        }
        // Using a DependencyProperty as the backing store for Template.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TemplateProperty =
            DependencyProperty.RegisterAttached("Template", typeof(ControlTemplate), typeof(LightBox), new PropertyMetadata(null));


        public static ItemsPanelTemplate GetItemsPanel(DependencyObject obj)
        {
            return (ItemsPanelTemplate)obj.GetValue(ItemsPanelProperty);
        }
        public static void SetItemsPanel(DependencyObject obj, ItemsPanelTemplate value)
        {
            obj.SetValue(ItemsPanelProperty, value);
        }
        // Using a DependencyProperty as the backing store for ItemsPanel.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemsPanelProperty =
            DependencyProperty.RegisterAttached("ItemsPanel", typeof(ItemsPanelTemplate), typeof(LightBox), new PropertyMetadata(null));


        public static Style GetItemContainerStyle(DependencyObject obj)
        {
            return (Style)obj.GetValue(ItemContainerStyleProperty);
        }
        public static void SetItemContainerStyle(DependencyObject obj, Style value)
        {
            obj.SetValue(ItemContainerStyleProperty, value);
        }
        // Using a DependencyProperty as the backing store for ItemContainerStyle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemContainerStyleProperty =
            DependencyProperty.RegisterAttached("ItemContainerStyle", typeof(Style), typeof(LightBox), new PropertyMetadata(null));

        #endregion
    }

    static class LightBoxExtensions
    {
        public static void Close(this FrameworkElement dialog)
        {
            ApplicationCommands.Close.Execute(dialog, null);
        }
    }


    /// <summary>
    /// 既存のUI上にLightBoxを被せて表示するためのAdorner定義
    /// </summary>
    class LightBoxAdorner : Adorner
    {
        private ItemsControl _root;

        public EventHandler AllDialogClosed;


        public LightBoxAdorner(UIElement adornedElement, UIElement element) : base(adornedElement)
        {
            var col = System.Windows.Media.Color.FromArgb(100, 0, 0, 0);
            var brush = new System.Windows.Media.SolidColorBrush(col);

            var root = new ItemsControl();

            // ココで各種テンプレートなどの設定
            var template = LightBox.GetTemplate(element);
            if (template != null)
            {
                root.Template = template;
            }

            var itemsPanel = LightBox.GetItemsPanel(element);
            if (itemsPanel != null)
            {
                root.ItemsPanel = itemsPanel;
            }

            var itemContainerStyle = LightBox.GetItemContainerStyle(element);
            if (itemContainerStyle != null)
            {
                root.ItemContainerStyle = itemContainerStyle;
            }

            this.AddVisualChild(root);

            this._root = root;
        }

        public void AddDialog(FrameworkElement dialog)
        {
            this._root.Items.Add(dialog);

            // 追加したダイアログに対して、ApplicationCommands.Closeのコマンドに対するハンドラを設定。
            dialog.CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, OnClose));

            this.InvalidateVisual();
        }

        public void RemoveDialog(FrameworkElement dialog)
        {
            this._root.Items.Remove(dialog);

            if (this._root.Items.Count == 0)
            {
                // このAdornerを消去するように依頼するイベントを発行する。
                AllDialogClosed?.Invoke(this, null);
            }
        }

        private void OnClose(object sender, ExecutedRoutedEventArgs e)
        {
            var item = sender as FrameworkElement;

            if (item != null)
            {
                this.RemoveDialog(item);
            }
        }

        protected override int VisualChildrenCount
        {
            get { return this._root == null ? 0 : 1; }
        }

        protected override Visual GetVisualChild(int index)
        {
            if (index != 0) throw new ArgumentOutOfRangeException();
            return this._root;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            // ルートのグリッドはAdornerを付けている要素と同じサイズになるように調整
            this._root.Width = constraint.Width;
            this._root.Height = constraint.Height;
            this._root.Measure(constraint);
            return this._root.DesiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            this._root.Arrange(new Rect(new Point(0, 0), finalSize));
            return new Size(this._root.ActualWidth, this._root.ActualHeight);
        }
    }
}
