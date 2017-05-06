﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XamlCSS.CssParsing
{
    public class AstGenerator
    {
        private List<string> errors;
        private List<CssToken> tokens;
        private int currentIndex;
        private CssNode currentNode;
        private CssToken currentToken => tokens[currentIndex];
        private CssToken nextToken => tokens[currentIndex + 1];
        private CssNode n;

        private void ReadKeyframes()
        {
            throw new NotSupportedException();
        }

        private void AddError(string message, CssToken token)
        {
            errors.Add($"{message} ({token.Line}:{token.Column})");
        }

        private void ReadImport()
        {
            SkipWhitespace();

            switch (currentToken.Type)
            {
                case CssTokenType.DoubleQuotes:
                    currentIndex++;
                    ReadDoubleQuoteText(false);
                    SkipExpected(CssTokenType.Semicolon);

                    AddImportedStyle(currentNode);
                    break;
                case CssTokenType.SingleQuotes:
                    currentIndex++;
                    ReadSingleQuoteText(false);
                    SkipExpected(CssTokenType.Semicolon);

                    AddImportedStyle(currentNode);
                    break;
                default:
                    AddError($"ReadImport: unexpected token '{currentToken.Text}'", currentToken);
                    SkipUntilLineEnd();
                    break;
            }


        }

        private void GoToParent()
        {
            currentNode = currentNode.Parent;
        }

        private void AddOnParentAndSetCurrent(CssNode node)
        {
            node.Parent = currentNode.Parent;

            currentNode.Parent.Children.Add(node);
            currentNode = node;
        }

        private void AddOnParentAndSetCurrent(CssNodeType type)
        {
            AddOnParentAndSetCurrent(new CssNode(type));
        }

        private void AddAndSetCurrent(CssNode node)
        {
            node.Parent = currentNode;

            currentNode.Children.Add(node);
            currentNode = node;
        }
        private void AddAndSetCurrent(CssNodeType type)
        {
            AddAndSetCurrent(new CssNode(type));
        }

        private void ReadNamespaceDeclaration()
        {
            // current node is NamespaceDeclaration

            var oldParent = currentNode.Parent;

            SkipWhitespace();

            AddAndSetCurrent(CssNodeType.NamespaceKeyword);

            currentIndex++;

            ReadIdentifier(); // namespace keyword

            SkipWhitespace();

            AddOnParentAndSetCurrent(CssNodeType.NamespaceAlias);

            if (currentToken.Type != CssTokenType.DoubleQuotes &&
                currentToken.Type != CssTokenType.SingleQuotes)
            {
                ReadIdentifier(); // namespace alias

                SkipWhitespace();
            }

            AddOnParentAndSetCurrent(CssNodeType.NamespaceValue);

            switch (currentToken.Type)
            {
                case CssTokenType.DoubleQuotes:
                    currentIndex++;

                    ReadDoubleQuoteText(false);
                    SkipExpected(CssTokenType.Semicolon);

                    GoToParent();
                    break;
                case CssTokenType.SingleQuotes:
                    currentIndex++;

                    ReadSingleQuoteText(false);
                    SkipExpected(CssTokenType.Semicolon);
                    GoToParent();
                    break;
                default:
                    AddError($"ReadNamespaceDeclaration: unexpected token '{currentToken.Text}'", currentToken);
                    SkipUntilLineEnd();

                    currentNode = oldParent;
                    break;
            }
        }

        private void SkipExpected(CssTokenType type)
        {
            if (currentToken.Type != type)
            {
                throw new Exception("");
            }

            // currentNode.TextBuilder.Append(currentToken.Text);
            currentIndex++;
        }

        private void ReadIdentifier()
        {
            if (currentToken.Type != CssTokenType.Identifier)
            {
                throw new Exception("");
            }

            currentNode.TextBuilder.Append(currentToken.Text);
            currentIndex++;
        }

        private void SkipWhitespace()
        {
            while (currentIndex < tokens.Count &&
                (currentToken.Type == CssTokenType.Whitespace ||
                (currentToken.Type == CssTokenType.Slash &&
                       nextToken.Text == "*") ||
                       (currentToken.Type == CssTokenType.Slash &&
                    nextToken.Text == "/")))
            {
                if (currentToken.Type == CssTokenType.Slash &&
                       nextToken.Text == "*")
                {
                    SkipInlineCommentText();
                }
                else if (currentToken.Type == CssTokenType.Slash &&
                    nextToken.Text == "/")
                {
                    SkipLineCommentText();
                }
                else
                {
                    currentIndex++;
                }
            }
        }

        private void SkipUntilLineEnd()
        {
            while (currentIndex < tokens.Count &&
                currentToken.Text != "\n")
            {
                currentIndex++;
            }
        }

        private void ReadDocument()
        {
            SkipWhitespace();

            while (currentIndex < tokens.Count)
            {
                switch (currentToken.Type)
                {
                    case CssTokenType.Slash:
                        if (nextToken.Text == "/")
                        {
                            SkipLineCommentText();
                        }
                        else if (nextToken.Text == "*")
                        {
                            SkipInlineCommentText();
                        }
                        break;
                    case CssTokenType.At:
                        var identifier = nextToken;// Peek(tokens, currentIndex, CssTokenType.Identifier);

                        if (identifier.Text == "keyframes")
                        {
                            AddAndSetCurrent(CssNodeType.KeyframesDeclaration);

                            ReadKeyframes();

                            GoToParent();
                        }
                        else if (identifier.Text == "import")
                        {
                            AddAndSetCurrent(CssNodeType.ImportDeclaration);

                            currentIndex++;
                            currentIndex++;

                            ReadImport();

                            GoToParent();
                        }
                        else if (identifier.Text == "namespace")
                        {
                            AddAndSetCurrent(CssNodeType.NamespaceDeclaration);

                            ReadNamespaceDeclaration();

                            GoToParent();
                        }
                        else if (identifier.Text == "mixin")
                        {
                            AddAndSetCurrent(CssNodeType.MixinDeclaration);

                            currentIndex++;
                            currentIndex++;

                            ReadMixin();

                            GoToParent();
                        }
                        else
                        {
                            AddError($"ReadDocument: unexpected token '{identifier.Text}'", identifier);
                        }
                        break;
                    case CssTokenType.Dollar:
                        AddAndSetCurrent(CssNodeType.VariableDeclaration);

                        ReadVariable();

                        GoToParent();
                        break;
                    case CssTokenType.Identifier:
                    case CssTokenType.Dot:
                    case CssTokenType.Hash:
                    case CssTokenType.SquareBracketOpen:
                        AddAndSetCurrent(CssNodeType.StyleRule);

                        ReadStyleRule();

                        GoToParent();
                        break;
                }
                currentIndex++;

                SkipWhitespace();
            }
        }

        private void ReadMixin()
        {
            SkipWhitespace();

            ReadUntil(CssTokenType.ParenthesisOpen);
            TrimCurrentNode();

            currentIndex++;

            AddAndSetCurrent(CssNodeType.MixinParameters);

            while (currentToken.Type != CssTokenType.ParenthesisClose)
            {
                SkipWhitespace();

                if (currentNode.Type == CssNodeType.MixinParameters)
                {
                    AddAndSetCurrent(CssNodeType.MixinParameter);
                }
                else
                {
                    AddOnParentAndSetCurrent(CssNodeType.MixinParameter);
                }

                ReadUntil(CssTokenType.ParenthesisClose, CssTokenType.Comma, CssTokenType.Colon);

                if (currentToken.Type == CssTokenType.Colon)
                {
                    currentIndex++;
                    SkipWhitespace();

                    AddAndSetCurrent(CssNodeType.MixinParameterDefaultValue);

                    if (currentToken.Type == CssTokenType.DoubleQuotes)
                    {
                        currentIndex++;
                        ReadDoubleQuoteText(false);
                        ReadUntil(CssTokenType.ParenthesisClose, CssTokenType.Comma);
                    }
                    else if (currentToken.Type == CssTokenType.SingleQuotes)
                    {
                        currentIndex++;
                        ReadSingleQuoteText(false);
                        ReadUntil(CssTokenType.ParenthesisClose, CssTokenType.Comma);
                    }
                    else
                    {
                        ReadUntil(CssTokenType.ParenthesisClose, CssTokenType.Comma);
                    }

                    TrimCurrentNode();

                    GoToParent();
                }

                if (currentToken.Type != CssTokenType.ParenthesisClose)
                {
                    currentIndex++;
                }

                SkipWhitespace();

                GoToParent();
            }

            GoToParent();

            SkipIfFound(CssTokenType.ParenthesisClose);

            SkipWhitespace();

            SkipExpected(CssTokenType.BraceOpen);

            AddAndSetCurrent(CssNodeType.StyleDeclarationBlock);

            ReadStyleDeclarationBlock();

            GoToParent();

            SkipWhitespace();
        }

        private void ReadVariable()
        {
            SkipWhitespace();

            AddAndSetCurrent(CssNodeType.VariableName);

            ReadUntil(CssTokenType.Colon);
            TrimCurrentNode();

            currentIndex++;

            AddOnParentAndSetCurrent(CssNodeType.VariableValue);

            SkipWhitespace();

            if (currentToken.Type == CssTokenType.DoubleQuotes)
            {
                currentIndex++;
                ReadDoubleQuoteText(false);
                ReadUntil(CssTokenType.Semicolon);
            }
            else if (currentToken.Type == CssTokenType.SingleQuotes)
            {
                currentIndex++;
                ReadSingleQuoteText(false);
                ReadUntil(CssTokenType.Semicolon);
            }
            else
            {
                ReadUntil(CssTokenType.Semicolon);
            }

            TrimCurrentNode();
            currentIndex++;

            GoToParent();
        }

        private void ReadStyleRule()
        {
            AddAndSetCurrent(CssNodeType.Selectors);

            ReadSelectors();

            AddAndSetCurrent(CssNodeType.StyleDeclarationBlock);

            currentIndex++;

            ReadStyleDeclarationBlock();
            GoToParent();
        }

        private void ReadStyleDeclarationBlock()
        {
            SkipWhitespace();

            SkipIfFound(CssTokenType.BraceOpen);

            SkipWhitespace();

            while (currentToken.Type != CssTokenType.BraceClose)
            {
                SkipWhitespace();

                if (currentToken.Text[0] == '$')
                {
                    AddAndSetCurrent(CssNodeType.VariableDeclaration);

                    ReadVariable();

                    GoToParent();
                }
                else if (
                    currentNode.Parent.Type == CssNodeType.StyleRule &&
                    (
                        currentToken.Text[0] == '&' ||
                        currentToken.Text[0] == '.' ||
                        (
                            currentToken.Text[0] != '@' &&
                            FirstTokenTypeOf(tokens, currentIndex, new[] { CssTokenType.Semicolon, CssTokenType.BraceOpen, CssTokenType.DoubleQuotes, CssTokenType.SingleQuotes}) == CssTokenType.BraceOpen)))
                {
                    AddAndSetCurrent(CssNodeType.StyleRule);

                    ReadStyleRule();

                    GoToParent();
                }
                else if (currentToken.Text[0] == '@')
                {
                    var identifier = nextToken.Text;

                    if (identifier == "include")
                    {
                        currentIndex++;
                        currentIndex++;

                        AddAndSetCurrent(CssNodeType.MixinInclude);

                        ReadMixinInclude();

                        GoToParent();
                    }
                    else if (identifier == "Property")
                    {
                        currentIndex++;
                        currentIndex++;

                        AddAndSetCurrent(CssNodeType.PropertyTrigger);

                        ReadPropertyTrigger();

                        GoToParent();
                    }
                    else if (identifier == "Data")
                    {
                        currentIndex++;
                        currentIndex++;

                        AddAndSetCurrent(CssNodeType.DataTrigger);

                        ReadDataTrigger();

                        GoToParent();
                    }
                    else if (identifier == "Event")
                    {
                        currentIndex++;
                        currentIndex++;

                        AddAndSetCurrent(CssNodeType.EventTrigger);

                        ReadEventTrigger();

                        GoToParent();
                    }
                    else if (identifier == "Enter")
                    {
                        currentIndex++;
                        currentIndex++;

                        SkipWhitespace();

                        SkipExpected(CssTokenType.Colon);

                        currentIndex++;

                        AddAndSetCurrent(CssNodeType.EnterAction);

                        ReadEnterOrExitAction();

                        GoToParent();
                    }
                    else if (identifier == "Exit")
                    {
                        currentIndex++;
                        currentIndex++;

                        SkipWhitespace();

                        SkipExpected(CssTokenType.Colon);

                        currentIndex++;

                        AddAndSetCurrent(CssNodeType.ExitAction);

                        ReadEnterOrExitAction();

                        GoToParent();
                    }
                    else
                    {
                        AddError($"ReadStyleDeclarationBlock: '@{identifier}' not supported!", currentToken);
                        currentIndex++;
                    }
                }
                else
                {
                    if (currentNode.Type == CssNodeType.StyleDeclarationBlock)
                    {
                        AddAndSetCurrent(CssNodeType.StyleDeclaration);
                    }
                    else
                    {
                        AddOnParentAndSetCurrent(CssNodeType.StyleDeclaration);
                    }

                    AddAndSetCurrent(CssNodeType.Key);

                    ReadUntil(CssTokenType.Colon);
                    currentIndex++;

                    TrimCurrentNode();

                    AddOnParentAndSetCurrent(CssNodeType.Value);

                    SkipWhitespace();

                    if (currentToken.Type == CssTokenType.DoubleQuotes)
                    {
                        currentIndex++;
                        ReadDoubleQuoteText(false);
                        ReadUntil(CssTokenType.Semicolon);
                    }
                    else if (currentToken.Type == CssTokenType.SingleQuotes)
                    {
                        currentIndex++;
                        ReadSingleQuoteText(false);
                        ReadUntil(CssTokenType.Semicolon);
                    }
                    else
                    {
                        ReadUntil(CssTokenType.Semicolon);
                    }

                    currentIndex++;

                    TrimCurrentNode();

                    if (currentNode.TextBuilder.Length > 0 &&
                        currentNode.Text[0] == '$')
                    {
                        var variable = currentNode.Text;
                        currentNode.TextBuilder.Clear();
                        AddAndSetCurrent(CssNodeType.VariableReference);
                        currentNode.TextBuilder.Append(variable);
                        GoToParent();
                    }

                    SkipWhitespace();

                    GoToParent();
                    GoToParent();
                }

                SkipWhitespace();
            }


            SkipWhitespace();

        }

        private void ReadEnterOrExitAction()
        {
            SkipWhitespace();

            AddAndSetCurrent(CssNodeType.ActionDeclarationBlock);

            ReadActionDeclarationBlock();

            GoToParent();
        }

        private void ReadActionDeclarationBlock()
        {
            SkipWhitespace();
            SkipExpected(CssTokenType.BraceOpen);
            SkipWhitespace();

            while (currentToken.Type != CssTokenType.BraceClose)
            {
                SkipWhitespace();

                AddAndSetCurrent(CssNodeType.ActionDeclaration);
                AddAndSetCurrent(CssNodeType.Key);

                ReadUntil(CssTokenType.Colon);
                currentIndex++;
                TrimCurrentNode();

                AddOnParentAndSetCurrent(CssNodeType.ActionParameterBlock);

                ReadActionParameterBlock();

                GoToParent();

                if (currentToken.Type != CssTokenType.BraceClose)
                {
                    currentIndex++;
                }

                SkipWhitespace();

                GoToParent();
            }

            SkipExpected(CssTokenType.BraceClose);
        }

        private void ReadActionParameterBlock()
        {
            SkipWhitespace();
            SkipExpected(CssTokenType.BraceOpen);
            SkipWhitespace();

            while (currentToken.Type != CssTokenType.BraceClose)
            {
                SkipWhitespace();

                AddAndSetCurrent(CssNodeType.ActionParameter);
                AddAndSetCurrent(CssNodeType.Key);

                ReadUntil(CssTokenType.Colon);
                currentIndex++;
                TrimCurrentNode();

                AddOnParentAndSetCurrent(CssNodeType.Value);

                SkipWhitespace();

                if (currentToken.Type == CssTokenType.DoubleQuotes)
                {
                    currentIndex++;
                    ReadDoubleQuoteText(false);
                    ReadUntil(CssTokenType.Semicolon);
                }
                else if (currentToken.Type == CssTokenType.SingleQuotes)
                {
                    currentIndex++;
                    ReadSingleQuoteText(false);
                    ReadUntil(CssTokenType.Semicolon);
                }
                else
                {
                    ReadUntil(CssTokenType.Semicolon);
                }

                if (currentToken.Type != CssTokenType.BraceClose)
                {
                    currentIndex++;
                }

                SkipWhitespace();

                GoToParent();
                GoToParent();
            }
            currentIndex++;
        }
        

        private void ReadPropertyTrigger()
        {
            SkipWhitespace();

            AddAndSetCurrent(CssNodeType.PropertyTriggerProperty);
            ReadUntil(CssTokenType.Whitespace);
            currentIndex++;
            TrimCurrentNode();

            AddOnParentAndSetCurrent(CssNodeType.PropertyTriggerValue);

            if (currentToken.Type == CssTokenType.DoubleQuotes)
            {
                currentIndex++;
                ReadDoubleQuoteText(false);
                ReadUntil(CssTokenType.Whitespace);
            }
            else if (currentToken.Type == CssTokenType.SingleQuotes)
            {
                currentIndex++;
                ReadSingleQuoteText(false);
                ReadUntil(CssTokenType.Whitespace);
            }
            else
            {
                ReadUntil(CssTokenType.Whitespace);
            }

            currentIndex++;
            TrimCurrentNode();

            AddOnParentAndSetCurrent(CssNodeType.StyleDeclarationBlock);

            ReadStyleDeclarationBlock();

            GoToParent();
        }

        private void ReadDataTrigger()
        {
            SkipWhitespace();

            AddAndSetCurrent(CssNodeType.DataTriggerBinding);
            ReadUntil(CssTokenType.Whitespace);
            currentIndex++;
            TrimCurrentNode();

            AddOnParentAndSetCurrent(CssNodeType.DataTriggerValue);

            if (currentToken.Type == CssTokenType.DoubleQuotes)
            {
                currentIndex++;
                ReadDoubleQuoteText(false);
                ReadUntil(CssTokenType.Whitespace);
            }
            else if (currentToken.Type == CssTokenType.SingleQuotes)
            {
                currentIndex++;
                ReadSingleQuoteText(false);
                ReadUntil(CssTokenType.Whitespace);
            }
            else
            {
                ReadUntil(CssTokenType.Whitespace);
            }

            currentIndex++;
            TrimCurrentNode();

            AddOnParentAndSetCurrent(CssNodeType.StyleDeclarationBlock);

            ReadStyleDeclarationBlock();

            GoToParent();
        }

        private void ReadEventTrigger()
        {
            SkipWhitespace();

            AddAndSetCurrent(CssNodeType.EventTriggerEvent);
            ReadUntil(CssTokenType.Whitespace);
            currentIndex++;
            TrimCurrentNode();

            AddOnParentAndSetCurrent(CssNodeType.ActionDeclarationBlock);

            ReadActionDeclarationBlock();

            GoToParent();
        }

        private void ReadMixinInclude()
        {
            SkipWhitespace();

            ReadUntil(CssTokenType.ParenthesisOpen, CssTokenType.Semicolon);
            TrimCurrentNode();

            AddAndSetCurrent(CssNodeType.MixinIncludeParameters);

            if (currentToken.Type == CssTokenType.ParenthesisOpen)
            {

                SkipExpected(CssTokenType.ParenthesisOpen);


                while (currentToken.Type != CssTokenType.ParenthesisClose)
                {
                    SkipWhitespace();

                    if (currentNode.Type == CssNodeType.MixinIncludeParameters)
                    {
                        AddAndSetCurrent(CssNodeType.MixinIncludeParameter);
                    }
                    else
                    {
                        AddOnParentAndSetCurrent(CssNodeType.MixinIncludeParameter);
                    }

                    ReadUntil(CssTokenType.ParenthesisClose, CssTokenType.Comma);
                    TrimCurrentNode();

                    if (currentToken.Type != CssTokenType.ParenthesisClose)
                    {
                        currentIndex++;
                    }

                    SkipWhitespace();

                    GoToParent();
                }

                SkipIfFound(CssTokenType.ParenthesisClose);
            }

            SkipExpected(CssTokenType.Semicolon);

            GoToParent();
        }

        private void ReadSelectors()
        {
            while (currentToken.Type != CssTokenType.BraceOpen)
            {
                SkipWhitespace();

                if (currentNode.Type == CssNodeType.Selectors)
                {
                    AddAndSetCurrent(CssNodeType.Selector);
                }
                else
                {
                    AddOnParentAndSetCurrent(CssNodeType.Selector);
                }

                while (currentToken.Type != CssTokenType.BraceOpen &&
                    currentToken.Type != CssTokenType.Comma)
                {

                    if (currentNode.Type == CssNodeType.Selector)
                    {
                        AddAndSetCurrent(CssNodeType.SelectorFragment);
                    }
                    else
                    {
                        AddOnParentAndSetCurrent(CssNodeType.SelectorFragment);
                    }

                    ReadUntil(CssTokenType.BraceOpen, CssTokenType.Comma, CssTokenType.Whitespace);

                    TrimCurrentNode();

                    SkipWhitespace();

                    GoToParent();
                }

                SkipIfFound(CssTokenType.Comma);
            }

            SkipWhitespace();

            GoToParent();
            GoToParent();
        }

        private void SkipIfFound(CssTokenType type)
        {
            if (currentToken.Type == type)
            {
                currentIndex++;
            }
        }

        private void ReadUntil(params CssTokenType[] types)
        {
            while (currentIndex < tokens.Count &&
                types.Contains(currentToken.Type) == false)
            {
                if (currentToken.Type == CssTokenType.Slash &&
                    nextToken.Text == "*")
                {
                    SkipInlineCommentText();
                }
                else if (currentToken.Type == CssTokenType.Slash &&
                    nextToken.Text == "/")
                {
                    SkipLineCommentText();
                }
                else
                {
                    currentNode.TextBuilder.Append(currentToken.Text);
                    currentIndex++;
                }
            }
        }

        public CssNode GetAst(string cssDocument)
        {
            errors = new List<string>();

            currentNode = new CssNode(CssNodeType.Document, null, "");

            currentIndex = 0;

            tokens = Tokenizer.Tokenize(cssDocument).ToList();

            ReadDocument();

            return currentNode;
        }

        private void TrimCurrentNode()
        {
            currentNode.TextBuilder = new StringBuilder(currentNode.Text.Trim());
        }


        private CssNode _ReadSemicolonAst()
        {
            TrimCurrentNode();

            if (currentNode.Type == CssNodeType.VariableReference)
            {
                currentNode = currentNode.Parent;
            }
            if (currentNode.Type == CssNodeType.Value)
            {
                currentNode = currentNode.Parent;
            }
            else if (currentNode.Type == CssNodeType.NamespaceValue)
            {
                currentNode = currentNode.Parent;
            }
            else if (currentNode.Type == CssNodeType.VariableValue)
            {
                currentNode = currentNode.Parent;
            }
            else if (currentNode.Type == CssNodeType.MixinParameterDefaultValue)
            {
                currentNode = currentNode.Parent;
            }

            TrimCurrentNode();

            currentNode = currentNode.Parent;

            return currentNode;
        }

        private void AddImportedStyle(CssNode currentNode)
        {
            var content = CssParser.cssFileProvider?.LoadFrom(currentNode.Text);

            if (content != null)
            {
                var ast = new AstGenerator().GetAst(content);

                var document = currentNode.Parent;

                document.Children.AddRange(ast.Children);
            }
        }

        private void ReadSingleQuoteText(bool goToParent = true)
        {
            do
            {
                if (tokens[currentIndex].Type == CssTokenType.Backslash)
                {
                    currentIndex++;
                    currentNode.TextBuilder.Append(tokens[currentIndex].Text);
                }
                else if (tokens[currentIndex].Type == CssTokenType.SingleQuotes)
                {
                    if (goToParent)
                    {
                        currentNode = currentNode.Parent;
                    }
                    currentIndex++;
                    break;
                }
                else
                {
                    currentNode.TextBuilder.Append(tokens[currentIndex].Text);
                }
                currentIndex++;
            } while (currentIndex < tokens.Count);
        }

        private void ReadDoubleQuoteText(bool goToParent = true)
        {
            do
            {
                if (tokens[currentIndex].Type == CssTokenType.Backslash)
                {
                    currentIndex++;
                    currentNode.TextBuilder.Append(tokens[currentIndex].Text);
                }
                else if (tokens[currentIndex].Type == CssTokenType.DoubleQuotes)
                {
                    if (goToParent)
                    {
                        currentNode = currentNode.Parent;
                    }
                    currentIndex++;
                    break;
                }
                else
                {
                    currentNode.TextBuilder.Append(tokens[currentIndex].Text);
                }
                currentIndex++;
            } while (currentIndex < tokens.Count);
        }

        private void SkipLineCommentText()
        {
            do
            {
                if (tokens[currentIndex].Type == CssTokenType.Whitespace &&
                    (tokens[currentIndex].Text == "\n" || tokens[currentIndex].Text == "\r"))
                {
                    break;
                }
                else
                {

                }
                currentIndex++;
            } while (currentIndex < tokens.Count);
        }

        private void SkipInlineCommentText()
        {
            do
            {
                if (tokens[currentIndex].Type == CssTokenType.Identifier &&
                    (tokens[currentIndex].Text == "*" && tokens[currentIndex + 1].Text == "/"))
                {
                    currentIndex++;
                    currentIndex++;
                    break;
                }
                else
                {

                }
                currentIndex++;
            } while (currentIndex < tokens.Count);
        }

        private static CssToken Peek(List<CssToken> tokens, int currentIndex, CssTokenType type = CssTokenType.Unknown)
        {
            currentIndex++;
            if (type == CssTokenType.Unknown)
            {
                if (currentIndex >= tokens.Count)
                {
                    return null;
                }

                return tokens[currentIndex];
            }

            while (currentIndex < tokens.Count)
            {
                var token = tokens[currentIndex];
                if (token.Type == type)
                {
                    return token;
                }

                currentIndex++;
            }

            return null;
        }

        private static CssTokenType FirstTokenTypeOf(List<CssToken> tokens, int index, CssTokenType[] types, bool ignoreWhitespace = true)
        {
            index++;

            while (index < tokens.Count)
            {
                if (ignoreWhitespace &&
                    tokens[index].Type == CssTokenType.Whitespace)
                {
                    index++;
                    continue;
                }

                foreach (var type in types)
                {
                    if (tokens[index].Type == type)
                    {
                        return type;
                    }
                }

                index++;
            }

            return CssTokenType.Unknown;
        }
    }
}
