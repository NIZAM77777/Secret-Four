using System;
using UnityEngine;

namespace MN7.Store
{
    [Serializable]
    public class MN7StoreCategoryData
    {
        [Tooltip("Stable ID used by Store Items.")]
        public string categoryId;

        [Tooltip("Text shown on the category button.")]
        public string displayName;

        [Tooltip("Lower values appear first. ALL is always first.")]
        public int sortOrder;
    }
}
