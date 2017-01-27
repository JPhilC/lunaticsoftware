using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
//for the popup
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Media.Effects;

namespace ASCOM.Lunatic.TelescopeDriver
{
    public class TextBoxTemplate : TextBox
    {
        //Philosophy:
        //For input fields for time, direction, distance, speed, etc., special input limitations apply.
        //Rather than letting the user key in syntactically incorrect or out-of-range values and then displaying an error,
        //this textboxe prevents illegal input.  
        //The value of the box should be accessed only from the "Value" property.  Accessing the "Text" property throws an exception.
        //
        //Input:
        //The textbox displays a leading-zero string which can be edited in overstrike mode only
        //All non-valid input is ignored and beeped and an error popup is displayed 
        //
        //Template Format:
        //0000.000      as you'd expect (Leading and trailing zeros are always displayed)
        //450.000       number must be strictly less than 450 (up to two digits may be used)
        //any characters which is not a digit or '.' are "field delimiter" characters and are skipped over 
        //E             allows either E or W to be entered
        //N             allows either N or S to be entered
        //A             allows either A or P to be input (For AM/PM
        //ENA fields must not be directly adjacent to numer fields, there must be an intervening character
        // If there is a field with a '.', it must be the rightmost field
        //
        //The following Templates have been used in an application and are reasonably well tested.
        public static string LatTemplate = "90º60.00'N";
        public static string LongTemplate = "180º60.00'E";
        public static string SpeedTemplate = "00.0kts";
        public static string RangeTemplate = "00.000nm";
        public static string RangeTemplateYds = "00000yds";
        public static string RangeTemplateMtrs = "00000m";
        public static string BearingTemplate = "360º";
        public static string InclinationTemplate = "00.0ºE";
        public static string TimeTemplate = "000.0min";
        public static string MinutesTemplate = "00min";
        public static string TimeOfDayTemplate = "13:60:60.0AM";
        public static string ShortTimeOfDayTemplate = "13:60:60AM";
        public static string TimeOfDayTemplate24 = "24:60:60.0";
        public static string ShortTimeOfDayTemplate24 = "24:60:60";


        private bool handlingInternalSelection = false; //to prevent recursion when selection event causes a change in selection
        private bool movingLeft = false;
        private class field
        {
            public int fieldStart, fieldLength;
            public double maxValue;
            public bool truncate;
        }
        List<field> fields;
        string OKCharacters = "0123456789.ANE";
        string OKCharactersNoDot = "0123456789ANE";
        string OKDigitsAndDot = "0123456789.";

        protected string inputTemplate = "";
        protected void internalTextSet(string text)
        {
            base.Text = text;
        }
        public string InputTemplate
        {
            get { return inputTemplate; }
            //when the inputTemplate is set, it is parsed into fields
            set
            {
                fields.Clear();
                inputTemplate = value;
                //find the input fields--delimited by any character which is not a digit or '.'
                int charPos = 0;
                while (charPos < inputTemplate.Length - 1)
                {
                    //skip over label characters between fields
                    while (charPos < inputTemplate.Length && !OKDigitsAndDot.Contains(inputTemplate[charPos])) charPos++;
                    if (charPos < inputTemplate.Length - 1)
                    {
                        field f = new field();
                        f.fieldStart = charPos;
                        while (charPos < inputTemplate.Length && OKDigitsAndDot.Contains(inputTemplate[charPos])) charPos++;
                        f.fieldLength = charPos - f.fieldStart;
                        double.TryParse(inputTemplate.Substring(f.fieldStart, f.fieldLength), out f.maxValue);
                        f.truncate = true;
                        fields.Add(f);
                    }
                }
                //check to see if the last field is to be ronded or truncated
                field fLast = fields[fields.Count - 1];
                if (inputTemplate.Substring(fLast.fieldStart, fLast.fieldLength).IndexOf('.') == -1)
                {
                    fLast.truncate = false;
                }
                //find AM/PM E/W or N/S fields
                int ind = inputTemplate.IndexOfAny(new char[] { 'E', 'N', 'A' });
                if (ind != -1)
                {
                    field f = new field();
                    f.fieldStart = ind;
                    f.fieldLength = 1;
                    f.maxValue = -1;
                    fields.Add(f);
                }
                base.Text = GetTextFromValue(this.value); //update the text with the new template
            }
        }
        //This is here to throw an exception if code tries to write directly to the Text field which could cause a crash if
        //the template and text are not properly aligned
        public new string Text
        {
            get { return base.Text; ; }
            set { throw (new InvalidOperationException("Do not set/get the Text directly, use the 'value' field.")); }
        }

        private double value;
        public double Value
        {
            get
            {
                value = GetValueFromText(base.Text);
                return this.value;
            }
            set
            {
                this.value = value;
                base.Text = GetTextFromValue(value);
            }
        }
        public static double TimeToDouble(DateTime dt)
        {
            double val = dt.Hour + dt.Minute / 60.0 + dt.Second / 3600.0 + dt.Millisecond / 3600000.0;
            return val;
        }
        public static DateTime TimeFromDouble(double val1)
        {
            double val = val1;

            if (val < 0)
                return new DateTime(1, 1, 1, 0, 0, 0, 0);
            int hrs = (int)val;
            val = (val - hrs) * 60;
            int min = (int)val;
            val = (val - min) * 60;
            int sec = (int)val;
            val = (val - sec) * 1000;
            int milli = (int)val;
            DateTime dt = new DateTime(1, 1, 1, hrs, min, sec, milli);
            double ticks = dt.Ticks;
            ticks /= 1000000;
            ticks = Math.Round(ticks);
            ticks *= 1000000;
            dt = new DateTime((long)ticks);
            return dt;
        }

        protected double GetValueFromText(string text)
        {
            double val = 0;
            double divisor = 1;
            foreach (field f in fields)
            {
                if (f.maxValue != -1)
                {
                    double add;
                    double.TryParse(text.Substring(f.fieldStart, f.fieldLength), out add);
                    if (fields.IndexOf(f) != 0)
                    {
                        if (f.maxValue > 0)
                            divisor *= f.maxValue;
                    }
                    val = val + add / divisor;
                }
                else
                {
                    char c = text[f.fieldStart];
                    if (c == 'S' || c == 'W')
                        val = -val;
                    if (c == 'P')
                        val = val + 12;
                }
            }
            return val;
        }
        private string changeDigitsToChar(string input,char c)
        {
            StringBuilder tTemplate = new StringBuilder(input);
            for (int i = 0; i < tTemplate.Length; i++)
                if (Char.IsDigit(input, i))
                    tTemplate[i] = c;
            return tTemplate.ToString();
        }
        protected string GetTextFromValue(double theVal)
        {
            double val = theVal;
            StringBuilder valueAsString = new StringBuilder(inputTemplate);
            field f1 = fields[fields.Count - 1];
            if (f1.maxValue == -1)
            {
                if (inputTemplate[f1.fieldStart] == 'E')
                {
                    if (val < 0)
                    {
                        valueAsString[f1.fieldStart] = 'W';
                        val = -val;
                    }
                }
                if (inputTemplate[f1.fieldStart] == 'N')
                {
                    if (val < 0)
                    {
                        valueAsString[f1.fieldStart] = 'S';
                        val = -val;
                    }
                }
                if (inputTemplate[f1.fieldStart] == 'A')
                {
                    if (val > 12)
                    {
                        valueAsString[f1.fieldStart] = 'P';
                        if (val > 13)
                            val = val - 12.0;
                    }
                    if (val > 24)
                        val -= 24;
                }
            }
            foreach (field f in fields)
            {
                if (inputTemplate.Substring(f.fieldStart, f.fieldLength).Contains('.'))
                {
                    this.IsEnabled = true;
                    string fieldValue = val.ToString(changeDigitsToChar(inputTemplate.Substring(f.fieldStart, f.fieldLength),'0'));
                    if (fieldValue.Length == f.fieldLength)
                    {
                        valueAsString.Remove(f.fieldStart, f.fieldLength);
                        valueAsString.Insert(f.fieldStart, fieldValue);
                    }
                    else if (inputTemplate[0] == '0' && f.fieldStart == 0)
                    {
                        InputTemplate = new string('0', fieldValue.Length - f.fieldLength) + inputTemplate;
                        return GetTextFromValue(theVal) ;
                    }
                    else 
                    {
                        this.IsEnabled = false;
                        return changeDigitsToChar(InputTemplate, '*');
                    }
                }
                else if (f.maxValue != -1)
                {
                    int iVal,iVal1;
                    if (f.truncate)
                        iVal = (int)val;
                    else
                        iVal = (int)Math.Round(val);
                    iVal1 = iVal;
                    if (iVal1 > f.maxValue && f.maxValue != 0)
                        iVal1 = iVal1 % (int)f.maxValue;
                    valueAsString.Remove(f.fieldStart, f.fieldLength);
                    valueAsString.Insert(f.fieldStart,
                        iVal1.ToString(changeDigitsToChar(inputTemplate.Substring(f.fieldStart, f.fieldLength),'0')));
                    double remainder = val - iVal;
                    field fNext = null;
                    if (fields.IndexOf(f) < fields.Count - 1)
                    {
                        fNext = fields[fields.IndexOf(f) + 1];
                        if (fNext.maxValue != -1)
                            val = remainder * fNext.maxValue;
                    }
                }
            }
            return valueAsString.ToString();
        }

        public TextBoxTemplate()
        {
            this.PreviewKeyDown += new KeyEventHandler(TextBoxTemplate_PreviewKeyDown);
            this.SelectionChanged += new RoutedEventHandler(TextBoxTemplate_SelectionChanged);
            this.GotFocus += new RoutedEventHandler(TextBoxTemplate_GotFocus);
            this.AllowDrop = false;
            CharacterCasing = CharacterCasing.Upper;
            fields = new List<field>();
            this.ContextMenu = null;
        }

        //if the control got focus via a keyboard TAB, then position the cursor appropriately
        void TextBoxTemplate_GotFocus(object sender, RoutedEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                //position at the end of the box
                field lastField = fields[fields.Count - 1];
                base.CaretIndex = lastField.fieldStart + lastField.fieldLength - 1;
            }
            else if (Mouse.LeftButton == MouseButtonState.Released)
            {
                //position at the beginnning of the box
                base.CaretIndex = 0;
            }
        }

        void TextBoxTemplate_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (!handlingInternalSelection)
            {
                handlingInternalSelection = true;
                field lastField = fields[fields.Count - 1];
                while (SelectionStart > 0 && SelectionStart >= lastField.fieldStart + lastField.fieldLength)
                    SelectionStart--;
                if (!movingLeft && SelectionStart < base.Text.Length - 1 && !OKCharactersNoDot.Contains(inputTemplate[SelectionStart]))
                    SelectionStart++;
                else if (movingLeft && SelectionStart > 0)
                {
                    SelectionStart--;
                    if (!OKCharactersNoDot.Contains(inputTemplate[SelectionStart]) && SelectionStart > 0)
                        SelectionStart--;
                }
                if (SelectionStart > base.Text.Length - 1)
                    SelectionStart = base.Text.Length - 1;
                SelectionLength = 1;
                handlingInternalSelection = false;
            }
            movingLeft = false;
        }

        bool valueInRange(Key key, out string max)
        {
            bool valid = false;
            max = "";
            if (key >= Key.D0 && key <= Key.D9)
            {
                int digit = key - Key.D0;
                field theField = null;
                foreach (field f in fields)
                {
                    if (SelectionStart >= f.fieldStart && SelectionStart < f.fieldStart + f.fieldLength)
                    {
                        theField = f;
                        break;
                    }
                }
                StringBuilder sb = new StringBuilder(Text.Substring(theField.fieldStart, theField.fieldLength));
                sb[SelectionStart - theField.fieldStart] = (char)('0' + (char)digit);
                double val;
                double.TryParse(sb.ToString(), out val);
                if (val < theField.maxValue || theField.maxValue == 0.0)
                    valid = true;
                else
                { //will zeroing the subsequent digit make it ligit?
                    if (SelectionStart - theField.fieldStart + 1 < sb.Length)
                    {
                        int index = SelectionStart - theField.fieldStart + 1;
                        sb[index] = '0';
                        double.TryParse(sb.ToString(), out val);
                        if (val < theField.maxValue)
                        {
                            base.Text = base.Text.Remove(theField.fieldStart, theField.fieldLength);
                            base.Text = base.Text.Insert(theField.fieldStart, sb.ToString());
                            valid = true;
                        }
                    }
                }
                max = theField.maxValue.ToString();
            }
            return valid;
        }

        void TextBoxTemplate_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            string errorMessage = "Invalid Key";
            Key theKey = e.Key;
            bool valid = false;
            //remap numeric keypad to regular keys for validation
            if (theKey >= Key.NumPad0 && theKey <= Key.NumPad9)
            {
                theKey = theKey - Key.NumPad0 + Key.D0;
            }

            //check for value out-of-range
            string maxValue = "";
            if (valueInRange(theKey, out maxValue))
                valid = true;
            else if (theKey >= Key.D0 && theKey <= Key.D9 && Char.IsDigit(inputTemplate[SelectionStart]))
                errorMessage = "Value must be less than " + maxValue;

            if (theKey == Key.Return) //treat a return like a tab
            {
                MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                valid = true;
                e.Handled = true;
            }
            if (theKey == Key.Up || theKey == Key.Down) valid = true;
            if (theKey == Key.Right) //allow the right cursor key
            {
                //if you're at the last character, replace with a TAB
                field lastField = fields[fields.Count - 1];
                int lastPostion = lastField.fieldStart + lastField.fieldLength - 1;
                if (SelectionStart == lastPostion)
                {
                    MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    e.Handled = true;
                    return;
                }
                valid = true;
            }
            else if (theKey == Key.Left) //allow the left cursor key 
            {
                if (SelectionStart == fields[0].fieldStart)
                {
                    MoveFocus(new TraversalRequest( FocusNavigationDirection.Previous));
                    e.Handled = true;
                    return;
                }
                movingLeft = true;
                valid = true;
            }
            else if (theKey == Key.OemPeriod || theKey == Key.Decimal)
            {
                int index = inputTemplate.IndexOf('.');
                if (index > -1)
                {
                    CaretIndex = index + 1;
                    valid = true;
                    e.Handled = true;
                }
            }
            else if (theKey == Key.Tab || theKey == Key.LeftShift || theKey == Key.RightShift || theKey == Key.LeftAlt || theKey == Key.System)
            {
                valid = true;
            }
            else if ((theKey == Key.A || theKey == Key.P) && inputTemplate[SelectionStart] == 'A') //to handle am/pm in time fields
            {
                valid = true;
            }
            else if ((theKey == Key.E || theKey == Key.W) && inputTemplate[SelectionStart] == 'E')
            {
                valid = true;
            }
            else if ((theKey == Key.N || theKey == Key.S) && inputTemplate[SelectionStart] == 'N')
            {
                valid = true;
            }
            if (theKey == Key.Back) //convert backspaces into left-arrows
            {
                if (SelectionStart > 0)
                {
                    movingLeft = true;
                    SelectionStart--;
                }
                else
                {
                    MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous));
                }
                e.Handled = true;
                valid = true;
            }
            else if (theKey == Key.Delete)
            {
                errorMessage = "Overstrike characters.  Deletion is not needed.";
            }

            if (!valid)
            {
                //use a popup for the error message
                Popup pp = new Popup();
                ToolTipService.SetShowDuration(pp, 100);
                TextBlock tb = new TextBlock() { Text = errorMessage };
                tb.Foreground = SystemColors.InfoTextBrush;
                tb.Background = SystemColors.InfoBrush;
                
                Border b1 = new Border() { Child = tb };
                pp.Child = b1;
                b1.BorderThickness = new Thickness(1);
                b1.BorderBrush = SystemColors.ActiveBorderBrush;
                
                pp.Height = 20;
                pp.PlacementTarget = this;
                pp.StaysOpen = false;
                Rect r = GetRectFromCharacterIndex(SelectionStart);
                pp.HorizontalOffset = r.Left;
                pp.IsOpen = true;
                DispatcherTimer dt = new DispatcherTimer();
                dt.Interval = new TimeSpan(0, 0, 2);
                dt.Tick += delegate(object sender1, EventArgs e1) { pp.IsOpen = false; dt.Stop(); };
                dt.Start();

                e.Handled = true;
                System.Media.SystemSounds.Beep.Play();
            }
        }
    }
}
