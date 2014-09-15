﻿using System;
using System.Collections.Generic;

namespace Sherpa.Library.Taxonomy.Model
{
    public class ShTerm : ShTermItemBase
    {
        public List<ShTerm> Terms { get; set; }
        public bool IsAvailableForTagging { get; set; }

        public ShTerm()
        {
            Terms = new List<ShTerm>();
            IsAvailableForTagging = true;
        }
        public ShTerm(Guid id, string title) : base(id, title)
        {
            Terms = new List<ShTerm>();
            IsAvailableForTagging = true;
        }
    }
}
