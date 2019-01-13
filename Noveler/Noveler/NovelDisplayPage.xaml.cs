using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xamarin.Forms;

namespace Noveler
{
    public partial class NovelDisplayPage : ContentPage, INotifyPropertyChanged
    {
        public NovelDisplayPage(string context)
        {
            InitializeComponent();
            BindingContext = this;

            this.Context = context;
        }

        private string _context;
        public string Context
        {
            get { return _context; }
            set
            {
                if(value != _context)
                {
                    _context = value;
                    OnPropertyChanged(nameof(Context)); 
                }
            }
        }
    }
}
