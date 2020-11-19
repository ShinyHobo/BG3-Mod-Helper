﻿/// <summary>
/// The game object model.
/// </summary>
namespace bg3_mod_packer.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class GameObject
    {
        public string MapKey { get; set; }
        public string ParentTemplateId { get; set; }
        public string Name { get; set; }
        public string DisplayNameHandle { get; set; }
        public string DisplayName { get; set; }
        public string DescriptionHandle { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public string Icon { get; set; }
        public string Stats { get; set; }
        public List<GameObject> Children { get; set; }

        /// <summary>
        /// Gets the full depth of the tree.
        /// </summary>
        public int Depth {
            get {
                if (Children.Count == 0)
                    return 0;
                return Children.Select(x => x.Depth).DefaultIfEmpty().Max() + 1;
            }
        }

        /// <summary>
        /// Gets a count of all children in the tree
        /// </summary>
        private int count {
            get {
                if (Children == null)
                    return 0;
                return Children.Sum(x => x.count) + Children.Count;
            }
        }

        /// <summary>
        /// Gets a count of all children in the tree, plus the parent.
        /// </summary>
        public int Count()
        {
            return count + 1;
        }

        /// <summary>
        /// Recursively searches through the game object's children to find matching object names.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <returns>The filtered game object.</returns>
        public GameObject Search(string filter)
        {
            var filteredList = new List<GameObject>();
            foreach (var subItem in Children)
            {
                var filterItem = subItem.Search(filter);
                if (filterItem != null)
                    filteredList.Add(filterItem);
            }
            var filterGo = (GameObject)MemberwiseClone();
            filterGo.Children = filteredList;
            if (filterGo.FindMatch(filter))
                return filterGo;
            else
            {
                foreach (var subItem in filterGo.Children)
                {
                    if (subItem.FindMatch(filter) || subItem.Children.Count > 0) // if children exist, it means that at least one had a match
                    {
                        return filterGo;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Finds a match to a game object property value.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <returns></returns>
        private bool FindMatch(string filter)
        {
            return Name.IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                   MapKey?.IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                   ParentTemplateId?.IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                   DisplayNameHandle?.IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                   DisplayName?.IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                   DescriptionHandle?.IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                   Description?.IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                   Icon?.IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                   Stats?.IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }
    }
}
