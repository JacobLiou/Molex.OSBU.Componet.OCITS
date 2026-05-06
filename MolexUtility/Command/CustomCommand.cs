using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Windows.Controls;

namespace MolexUtility.Command
{
    public class CustomCommand
    {        
        public static readonly RoutedUICommand ILTest;
        static CustomCommand()
        {
            InputGestureCollection inputs = new InputGestureCollection();
            inputs.Add(new KeyGesture(Key.R, ModifierKeys.Control, "Ctrl+R"));
            ILTest = new RoutedUICommand("IL Test", "ILTest", typeof(CustomCommand),inputs);
        }

        public CustomCommand()
        {

        }
    }
}
