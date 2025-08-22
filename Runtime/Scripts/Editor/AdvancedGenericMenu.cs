using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEditor;

namespace PuppyDragon.uNodyEditor
{
    public class AdvancedGenericMenu : AdvancedDropdown
    {
        public const float kDefaultMinWidth = 200f;
        public const float kDefaultMinHeight = 250f;
        public const float kDefaultMaxWidth = 300f;

        private string rootTitle;
        private List<AdvancedGenericMenuItem> items = new();

        public AdvancedGenericMenu(string rootTitle = "")
            : this(rootTitle, new AdvancedDropdownState())
        {
        }

        public AdvancedGenericMenu(string rootTitle, AdvancedDropdownState state) : base(state)
        {
            this.rootTitle = rootTitle;
            minimumSize = new Vector2(kDefaultMinWidth, kDefaultMinHeight);
        }
        private AdvancedGenericMenuItem FindOrCreateItem(string name, AdvancedGenericMenuItem currentRoot = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            AdvancedGenericMenuItem item = null;
            
            string[] paths = name.Split( '/' );
            if ( currentRoot == null )
            {
                item = items.FirstOrDefault(x => x != null && x.name == paths[0]);
                if ( item == null )
                    items.Add(item = new AdvancedGenericMenuItem(paths[0]));
            }
            else
            {
                item = currentRoot.children.OfType<AdvancedGenericMenuItem>().FirstOrDefault(x => x.name == paths[0]);
                if ( item == null )
                    currentRoot.AddChild(item = new AdvancedGenericMenuItem(paths[0]));
            }

            if ( paths.Length > 1 )
                return FindOrCreateItem(string.Join( "/", paths, 1, paths.Length - 1), item);

            return item;
        }

        private AdvancedGenericMenuItem FindParent(string name)
        {
            string[] paths = name.Split( '/' );
            return FindOrCreateItem(string.Join( "/", paths, 0, paths.Length - 1 ));
        }

        // Summary:
        //     Add a disabled item to the menu.
        //
        // Parameters:
        //   content:
        //     The GUIContent to display as a disabled menu item.
        public void AddDisabledItem( GUIContent content )
        {
            var item = FindOrCreateItem( content.text );
            item.Set(false, null, null);
        }

        public void AddItem( string name, bool isOn, GenericMenu.MenuFunction voidFunc )
            => AddItem( new GUIContent( name ), isOn, voidFunc);

        public void AddItem(GUIContent content, bool isOn, GenericMenu.MenuFunction voidFunc)
        {
            var item = FindOrCreateItem( content.text );
            item.Set(true, content.image as Texture2D, voidFunc);
        }

        public void AddItem(string name, bool isOn, GenericMenu.MenuFunction2 oneParamFunc, object userData)
            => AddItem(new GUIContent( name ), isOn, oneParamFunc, userData );

        public void AddItem(GUIContent content, bool isOn, GenericMenu.MenuFunction2 func, object userData)
        {
            var item = FindOrCreateItem(content.text);
            item.Set(true, content.image as Texture2D, func, userData);
        }

        //
        // Summary:
        //     Add a seperator item to the menu.
        //
        // Parameters:
        //   path:
        //     The path to the submenu, if adding a separator to a submenu. When adding a separator
        //     to the top level of a menu, use an empty string as the path.
        public void AddSeparator(string path = null)
        {
            var parent = string.IsNullOrWhiteSpace(path) ? null : FindParent(path);
            if (parent == null)
                items.Add(null);
            else
                parent.AddSeparator();
        }

        //
        // Summary:
        //     Show the menu at the given screen rect.
        //
        // Parameters:
        //   position:
        //     The position at which to show the menu.
        public void DropDown(Rect position)
        {
            //position.width = Mathf.Clamp(position.width, kDefaultMinWidth, kDefaultMaxWidth);
            Show(position);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem(rootTitle);
            
            foreach (var item in items)
            {
                if (item == null)
                    root.AddSeparator();
                else
                    root.AddChild(item);
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item is AdvancedGenericMenuItem gmItem )
                gmItem.Execute();
        }

        private class AdvancedGenericMenuItem : AdvancedDropdownItem
        {
            private GenericMenu.MenuFunction voidFunc;
            private GenericMenu.MenuFunction2 oneParamFunc;
            private object userData;

            public AdvancedGenericMenuItem(string name) : base(name) { }

            public AdvancedGenericMenuItem(string name, bool enabled, Texture2D icon, GenericMenu.MenuFunction voidFunc)
                : base(name)
            {
                Set(enabled, icon, voidFunc);
            }

            public AdvancedGenericMenuItem(string name, bool enabled, Texture2D icon, GenericMenu.MenuFunction2 oneParamFunc, object userData)
                : base(name)
            {
                Set(enabled, icon, oneParamFunc, userData);
            }

            public void Set(bool enabled, Texture2D icon, GenericMenu.MenuFunction voidFunc)
            {
                this.enabled = enabled;
                this.icon = icon;
                this.voidFunc = voidFunc;
            }

            public void Set(bool enabled, Texture2D icon, GenericMenu.MenuFunction2 oneParamFunc, object userData)
            {
                this.enabled = enabled;
                this.icon = icon;
                this.oneParamFunc = oneParamFunc;
                this.userData = userData;
            }

            public void Execute()
            {
                if (oneParamFunc != null)
                    oneParamFunc.Invoke(userData);
                else if (voidFunc != null)
                    voidFunc.Invoke();
            }
        }
    }
}