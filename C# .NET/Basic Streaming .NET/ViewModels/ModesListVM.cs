using Basic_Streaming.NET.Models;
using System.Collections.Generic;

namespace Basic_Streaming.NET.ViewModels
{
    /// <summary>
    /// View Model for representing sensor modes in the UI
    /// </summary>
    public class ModesListVM
    {
        public List<Mode> Modes { get; set; }

        public ModesListVM() { }
    }
}
