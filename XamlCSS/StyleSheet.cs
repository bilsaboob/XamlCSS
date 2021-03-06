﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using XamlCSS.CssParsing;

namespace XamlCSS
{
    public class StyleSheet : INotifyPropertyChanged
    {
        public static readonly StyleSheet Empty = new StyleSheet();

        public StyleSheet()
        {
            Id = Guid.NewGuid().ToString();
        }

        public static Func<object, object> GetParent { get; internal set; }
        public static Func<object, StyleSheet> GetStyleSheet { get; internal set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected List<CssNamespace> combinedNamespaces;
        protected StyleRuleCollection combinedRules;
        protected List<CssNamespace> localNamespaces = new List<CssNamespace>();
        protected StyleRuleCollection localRules = new StyleRuleCollection();
        protected List<StyleSheet> addedStyleSheets = null;
        protected bool inheritStyleSheets = true;
        protected List<StyleSheet> inheritedStyleSheets = null;
        protected string content = null;
        protected object attachedTo;

        virtual public List<CssNamespace> LocalNamespaces
        {
            get
            {
                return localNamespaces;
            }
            set
            {
                localNamespaces = value;
            }
        }

        virtual public StyleRuleCollection LocalRules
        {
            get
            {
                return localRules;
            }
            set
            {
                localRules = value;
            }
        }

        virtual public string Content
        {
            get
            {
                return content;
            }
            set
            {
                content = value;

                Reset();

                var sheet = CssParser.Parse(content);

                foreach (var error in sheet.Errors)
                {
                    this.Errors.Add(error);
                }
                foreach (var warning in sheet.Warnings)
                {
                    this.Warnings.Add(warning);
                }

                this.LocalNamespaces = sheet.LocalNamespaces;
                this.LocalRules = sheet.LocalRules;

                inheritedStyleSheets = null;

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Content"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Errors"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Warnings"));
            }
        }

        private ObservableCollection<string> errors = new ObservableCollection<string>();
        virtual public ObservableCollection<string> Errors
        {
            get
            {
                return errors;
            }
            set
            {
                errors = value;

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Errors"));
            }
        }

        private ObservableCollection<string> warnings = new ObservableCollection<string>();
        virtual public ObservableCollection<string> Warnings
        {
            get
            {
                return warnings;
            }
            set
            {
                warnings = value;

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Warnings"));
            }
        }

        public object AttachedTo
        {
            get
            {
                return attachedTo;
            }
            set
            {
                attachedTo = value;

                inheritedStyleSheets = null;

                Reset();
            }
        }

        public bool InheritStyleSheets
        {
            get { return inheritStyleSheets; }
            set
            {
                inheritStyleSheets = value;

                inheritedStyleSheets = null;

                Reset();
            }
        }

        public List<CssNamespace> Namespaces
        {
            get
            {
                return combinedNamespaces ?? (combinedNamespaces = GetCombinedNamespaces());
            }
        }

        public StyleRuleCollection Rules
        {
            get
            {
                return combinedRules ?? (combinedRules = new StyleRuleCollection(GetCombinedStyleRules()));
            }
        }

        public List<StyleSheet> AddedStyleSheets
        {
            get
            {
                return addedStyleSheets ?? (addedStyleSheets = new List<StyleSheet>());
            }

            set
            {
                addedStyleSheets = value;

                Reset();
            }
        }

        public List<StyleSheet> InheritedStyleSheets
        {
            get
            {
                return inheritedStyleSheets ?? (inheritedStyleSheets = InheritStyleSheets ? GetParentStyleSheets(AttachedTo).Reverse<StyleSheet>().ToList() : new List<StyleSheet>());
            }
        }

        public string Id { get; protected set; }

        protected List<StyleSheet> GetParentStyleSheets(object from)
        {
            List<StyleSheet> styleSheets = new List<StyleSheet>();

            if (from == null)
            {
                return styleSheets;
            }

            var current = GetParent(from);
            while (current != null)
            {
                var styleSheet = GetStyleSheet(current);
                if (styleSheet != null)
                {
                    styleSheets.Add(styleSheet);
                }
                current = GetParent(current);
            }

            return styleSheets;
        }

        protected List<CssNamespace> GetCombinedNamespaces()
        {
            if (AddedStyleSheets?.Count == 0 &&
                InheritedStyleSheets?.Count == 0)
            {
                return LocalNamespaces;
            }

            return InheritedStyleSheets
                .Select(x => x.Namespaces)
                .Concat(AddedStyleSheets.Select(x => x.Namespaces))
                .Aggregate((a, b) => a.Concat(b).ToList())
                .Concat(LocalNamespaces)
                .GroupBy(x => x.Alias)
                .Select(x => x.Last())
                .ToList();
        }

        protected List<StyleRule> GetCombinedStyleRules()
        {
            if (AddedStyleSheets?.Count == 0 &&
                InheritedStyleSheets?.Count == 0)
            {
                return LocalRules;
            }

            return InheritedStyleSheets
                    .Select(x => x.Rules.ToList())
                    .Concat(AddedStyleSheets.Select(x => x.Rules.ToList()))
                    .Aggregate((a, b) => a.Concat(b).ToList())
                    .Concat(LocalRules)
                    .GroupBy(x => x.SelectorString)
                    .Select(x => new StyleRule
                    {
                        Selectors = x.First().Selectors,
                        SelectorType = x.First().SelectorType,
                        DeclarationBlock = new StyleDeclarationBlock(GetMergedStyleDeclarations(x.ToList()), x.SelectMany(y => y.DeclarationBlock.Triggers))
                    })
                    .ToList();
        }

        protected List<StyleDeclaration> GetMergedStyleDeclarations(List<StyleRule> styleRules)
        {
            return styleRules
                .SelectMany(x => x.DeclarationBlock.ToList())
                .GroupBy(x => x.Property)
                .Select(x => x.Last())
                .ToList();
        }

        protected void Reset()
        {
            combinedRules = null;
            combinedNamespaces = null;

            this.Errors.Clear();
            this.Warnings.Clear();
        }

        internal void AddError(string error)
        {
            Errors.Add(error);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Errors"));
        }
    }
}
