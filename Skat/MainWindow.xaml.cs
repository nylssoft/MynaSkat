﻿using MynaSkat.Core;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MynaSkat
{
    public partial class MainWindow : Window
    {
        private SkatTable skatTable;
        private Dictionary<int, BitmapImage> bitmapCache;
        private BitmapImage bitmapBack;
        private bool showLastStitch = false;
        private bool computerPlay = false;
        private DateTime lastCardPlayed = DateTime.Now;
        private Player selectedPlayer = null;

        public MainWindow()
        {
            InitializeComponent();
            bitmapCache = new Dictionary<int, BitmapImage>();
            for (int idx = 0; idx < 32; idx++)
            {
                var str = $"Images/{idx:D2}.gif";
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(str, UriKind.Relative);
                bitmap.EndInit();
                bitmapCache[idx] = bitmap;
            }
            bitmapBack = new BitmapImage();
            bitmapBack.BeginInit();
            bitmapBack.UriSource = new Uri("Images/back.gif", UriKind.Relative);
            bitmapBack.EndInit();
        }

        private void SelectActivePlayer()
        {
            selectedPlayer = skatTable.GetActivePlayer();
        }

        private void UpdateStatus()
        {
            var viewPlayer = GetPlayer();
            if (viewPlayer == null) return;
            // sort and update cards for viewed player
            viewPlayer.SortCards();
            Render(viewPlayer);
            UpdateControls();
            // update game type for viewed player
            radioButtonGrand.IsChecked = viewPlayer.Game.Type == GameType.Grand;
            radioButtonNull.IsChecked = viewPlayer.Game.Type == GameType.Null;
            radioButtonClubs.IsChecked = viewPlayer.Game.Type == GameType.Color && viewPlayer.Game.Color == CardColor.Clubs;
            radioButtonSpades.IsChecked = viewPlayer.Game.Type == GameType.Color && viewPlayer.Game.Color == CardColor.Spades;
            radioButtonHearts.IsChecked = viewPlayer.Game.Type == GameType.Color && viewPlayer.Game.Color == CardColor.Hearts;
            radioButtonDiamonds.IsChecked = viewPlayer.Game.Type == GameType.Color && viewPlayer.Game.Color == CardColor.Diamonds;
            // update game type for viewed player
            checkBoxOuvert.IsChecked = viewPlayer.Game.Option.HasFlag(GameOption.Ouvert);
            checkBoxHand.IsChecked = viewPlayer.Game.Option.HasFlag(GameOption.Hand);
            checkBoxSchneider.IsChecked = viewPlayer.Game.Option.HasFlag(GameOption.Schneider);
            checkBoxSchwarz.IsChecked = viewPlayer.Game.Option.HasFlag(GameOption.Schwarz);
        }

        private void UpdateControls()
        {
            var player = GetPlayer();
            if (player == null) return;
            radioButtonGrand.IsEnabled = !skatTable.GameStarted;
            radioButtonNull.IsEnabled = !skatTable.GameStarted;
            radioButtonClubs.IsEnabled = !skatTable.GameStarted;
            radioButtonSpades.IsEnabled = !skatTable.GameStarted;
            radioButtonHearts.IsEnabled = !skatTable.GameStarted;
            radioButtonDiamonds.IsEnabled = !skatTable.GameStarted;

            checkBoxOuvert.IsEnabled = skatTable.CanSetOuvert(player);
            checkBoxHand.IsEnabled = skatTable.CanSetHand(player);
            checkBoxSchneider.IsEnabled = skatTable.CanSetSchneider(player);
            checkBoxSchwarz.IsEnabled = skatTable.CanSetSchwarz(player);

            if (computerPlay &&
                skatTable.GameStarted &&
                player == skatTable.CurrentPlayer &&
                player != skatTable.GamePlayer)
            {
                gridPlayCards.Visibility = Visibility.Hidden;
                gridLastStitch.Visibility = Visibility.Hidden;
            }
            else
            {
                gridPlayCards.Visibility = Visibility.Visible;
                gridLastStitch.Visibility = Visibility.Visible;
            }

            if (skatTable.GameStarted)
            {
                gridStitch.Visibility = Visibility.Visible;
                gridSkat.Visibility = Visibility.Hidden;
            }
            else
            {
                gridStitch.Visibility = Visibility.Hidden;
                gridSkat.Visibility = Visibility.Visible;
            }
        }

        private Player GetPlayer()
        {
            if (skatTable == null || skatTable.Players.Count < 3) return null;
            if (selectedPlayer == null)
            {
                return skatTable.GetActivePlayer();
            }
            return selectedPlayer;
        }

        private GameType GetGameType()
        {
            if (radioButtonGrand.IsChecked == true) return GameType.Grand;
            if (radioButtonNull.IsChecked == true) return GameType.Null;
            return GameType.Color;
        }

        private CardColor? GetGameColor()
        {
            if (radioButtonClubs.IsChecked == true) return CardColor.Clubs;
            if (radioButtonSpades.IsChecked == true) return CardColor.Spades;
            if (radioButtonHearts.IsChecked == true) return CardColor.Hearts;
            if (radioButtonDiamonds.IsChecked == true) return CardColor.Diamonds;
            return null;
        }

        private GameOption GetGameOption()
        {
            GameOption ret = GameOption.None;
            if (checkBoxOuvert.IsChecked == true) ret |= GameOption.Ouvert;
            if (checkBoxHand.IsChecked == true) ret |= GameOption.Hand;
            if (checkBoxSchneider.IsChecked == true) ret |= GameOption.Schneider;
            if (checkBoxSchwarz.IsChecked == true) ret |= GameOption.Schwarz;
            return ret;
        }

        private void Render(Player player)
        {
            if (skatTable != null && player != null)
            {
                RenderPlayers(player);
                RenderOuvertOrScoreCards(player);
                RenderSkat(player);
                RenderLastStitch(player);
                RenderPlayerCards(player);
                RenderButtonsAndHeader(player);
                RenderStitch();
            }
        }

        private void RenderPlayers(Player player)
        {
            var players = new List<Player>(skatTable.Players);
            players.Sort((p1, p2) => p1.Position.CompareTo(p2.Position));
            Player leftPlayer = skatTable.GetNextPlayer(player);
            Player rightPlayer = skatTable.GetNextPlayer(leftPlayer);
            foreach (var p in players)
            {
                var txt = $"{p.Name}, {p.GetPositionText()}";
                if (skatTable.GameStarted && skatTable.GamePlayer == p)
                {
                    txt += $", {p.Game.GetGameText()}";
                }
                txt += $", {p.Score} Punkte";
                if (player == p)
                {
                    txt += $", Spiel {skatTable.GameCounter}";
                }
                TextBlock tb;
                if (player == p)
                {
                    tb = textBlockViewPlayer;
                }
                else if (leftPlayer == p)
                {
                    tb = textBlockLeftPlayer;
                }
                else // if (rightPlayer == p)
                {
                    tb = textBlockRightPlayer;
                }
                tb.Text = txt;
                tb.Foreground = !skatTable.GameEnded && skatTable.GetActivePlayer() == p ? Brushes.Aqua : Brushes.White;
                tb.Tag = p;
            }
        }

        private void RenderButtonsAndHeader(Player player)
        {
            var stat = skatTable.GetPlayerStatus(player);
            // buttons
            gridButtons.Children.Clear();
            var p = new StackPanel { Orientation = Orientation.Horizontal };
            if (showLastStitch)
            {
                AddActionButton(p, "Letzten Stich zurücklegen", ButtonViewLastStitch_Click, null, 230);
            }
            else
            {
                if (skatTable.CanCollectStitch(player) && (!computerPlay || player == skatTable.GamePlayer))
                {
                    AddActionButton(p, "Sitch einsammeln", ButtonCollectStitch_Click, null, 200);
                }
                if (skatTable.CanViewLastStitch(player) && (!computerPlay || player == skatTable.GamePlayer))
                {
                    AddActionButton(p, "Letzten Stich zeigen", ButtonViewLastStitch_Click, null, 230);
                }
                if (skatTable.CanStartNewGame())
                {
                    AddActionButton(p, "Neues Spiel", ButtonNewGame_Click);
                }
                if (skatTable.CanGiveUp(player))
                {
                    AddActionButton(p, "Aufgeben", ButtonGiveUp_Click);
                }
                for (var idx = 0; idx < stat.ActionLabels.Count; idx++)
                {
                    AddActionButton(p, stat.ActionLabels[idx], ButtonAction_Click, stat.ActionTypes[idx], 150);
                }
                if (player == skatTable.GamePlayer)
                {
                    if (!computerPlay)
                    {
                        AddActionButton(p, "Computerspieler einschalten", ButtonComputerPlayer_Click, null, 250);
                    }
                    else
                    {
                        AddActionButton(p, "Computerspieler ausschalten", ButtonComputerPlayer_Click, null, 250);
                    }
                }
            }
            if (p.Children.Count > 0)
            {
                gridButtons.Children.Add(p);
            }
            // header
            textBlockStatus.Text = $"Hallo {player.Name}! {stat.Header}";
        }

        private void AddActionButton(StackPanel p, string label, RoutedEventHandler click, Object tag = null, int width = 120)
        {
            var b = new Button() { Content = label, Height = 35, Width = width, Tag = tag, Margin = new Thickness(0, 0, 10, 0) };
            b.Click += click;
            p.Children.Add(b);
        }

        private void RenderOuvertOrScoreCards(Player player)
        {
            List<Card> cards = null;
            if (skatTable.CanShowOuvertCards(player))
            {
                cards = new List<Card>(skatTable.GamePlayer.Cards);
            }
            else if (skatTable.GameEnded)
            {
                cards = new List<Card>(player.Stitches);
                if (player == skatTable.GamePlayer)
                {
                    cards.AddRange(skatTable.Skat);
                }
            }
            RenderCards(gridOuvertCards, cards);
        }

        private void RenderStitch()
        {
            List<Card> cards = null;
            if (!showLastStitch)
            {
                cards = skatTable.Stitch;
            }
            RenderCards(gridStitch, cards, ImageStitch_MouseDown);
        }

        private void RenderLastStitch(Player player)
        {
            List<Card> cards = null;
            if (showLastStitch && skatTable.CanViewLastStitch(player))
            {
                cards = skatTable.LastStitch;
            }
            RenderCards(gridLastStitch, cards, ImageLastStitch_MouseDown);
        }

        private void RenderSkat(Player player)
        {
            var canPickup = skatTable.CanPickupSkat(player);
            List<Card> skat = null;
            if (!skatTable.SkatTaken || canPickup)
            {
                skat = skatTable.Skat;
            }
            RenderCards(gridSkat, skat, ImageSkat_MouseDown, !canPickup);
        }

        private void RenderPlayerCards(Player player)
        {
            RenderCards(gridPlayCards, player?.Cards, ImagePlayerCard_MouseDown);
        }

        private void RenderCards(Grid grid, List<Card> cards, MouseButtonEventHandler mouseDown = null, bool showBack = false)
        {
            grid.Children.Clear();
            var cnt = cards?.Count;
            if (cnt > 0)
            {
                StackPanel p = new StackPanel();
                p.HorizontalAlignment = HorizontalAlignment.Left;
                p.Orientation = Orientation.Horizontal;
                int width = 90;
                int height = 140;
                double rw = grid.ActualWidth - width / 2;
                double v = (rw - width * cnt.Value) / cnt.Value;
                if (v > 10.0)
                {
                    v = 10.0;
                }
                bool first = true;
                foreach (var card in cards)
                {
                    var image = new Image();
                    image.Source = showBack ? bitmapBack : bitmapCache[card.InternalNumber];
                    if (first)
                    {
                        image.Margin = new Thickness(0, 0, 0, 0);
                        first = false;
                    }
                    else
                    {
                        image.Margin = new Thickness(v, 0, 0, 0);
                    }
                    image.Width = width;
                    image.Height = height;
                    if (mouseDown != null)
                    {
                        image.MouseDown += mouseDown;
                    }
                    image.Tag = card;
                    if (!showBack)
                    {
                        image.ToolTip = card.ToString();
                    }
                    p.Children.Add(image);
                }
                grid.Children.Add(p);
            }
        }

        // callbacks

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            skatTable = new SkatTable("Niels", "Michael", "Bernhard");
            UpdateStatus();
            var timer = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, 1)
            };
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Render(GetPlayer());
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!computerPlay ||
                (DateTime.Now - lastCardPlayed).TotalSeconds < 5)
            {
                return;
            }
            if (skatTable.GameStarted &&  skatTable.CurrentPlayer != skatTable.GamePlayer)
            {
                if (skatTable.CurrentPlayer.Cards.Count > 0)
                {
                    var deck = new List<Card>();
                    deck.AddRange(skatTable.CurrentPlayer.Cards);
                    using var prng = new RNGCryptoServiceProvider();
                    while (deck.Count > 0)
                    {
                        var card = Card.DrawOne(prng, deck);
                        if (skatTable.IsValidForStitch(card))
                        {
                            skatTable.PlayCard(skatTable.CurrentPlayer, card);
                            lastCardPlayed = DateTime.Now;
                            break;
                        }
                    }
                }
                else if (skatTable.CanCollectStitch(skatTable.CurrentPlayer))
                {
                    skatTable.CollectStitch(skatTable.CurrentPlayer);
                    lastCardPlayed = DateTime.Now;
                }
                UpdateStatus();
            }
        }

        private void RadioButtonPlayer_Click(object sender, RoutedEventArgs e)
        {
            var player = GetPlayer();
            if (player == null) return;
            showLastStitch = false;
            UpdateStatus();
        }

        private void RadioButtonGameType_Click(object sender, RoutedEventArgs e)
        {
            var player = GetPlayer();
            if (player == null || skatTable.GameStarted) return;
            checkBoxOuvert.IsChecked = false;
            checkBoxHand.IsChecked = false;
            checkBoxSchneider.IsChecked = false;
            checkBoxSchwarz.IsChecked = false;
            player.Game = new Game(GetGameType(), GetGameColor())
            {
                Option = GetGameOption()
            };
            UpdateStatus();
        }

        private void CheckBoxOption_Click(object sender, RoutedEventArgs e)
        {
            var player = GetPlayer();
            if (player == null) return;
            skatTable.SetGameOption(player, GetGameOption());
            UpdateStatus();
        }

        private void ButtonNewGame_Click(object sender, RoutedEventArgs e)
        {
            showLastStitch = false;
            computerPlay = false;
            skatTable.StartNewRound();
            foreach (var player in skatTable.Players)
            {
                player.SortCards();
            }
            SelectActivePlayer();
            UpdateStatus();
        }

        private void ButtonAction_Click(object sender, RoutedEventArgs e)
        {
            var player = GetPlayer();
            var button = sender as Button;
            if (player == null ||
                button == null ||
                button.Visibility == Visibility.Hidden ||
                skatTable.GamePlayer != null && (skatTable.GamePlayer != player || skatTable.Skat.Count < 2))
            {
                return;
            }
            var playerAction = button.Tag as ActionType?;
            if (playerAction != null)
            {
                skatTable.PerformPlayerAction(player, playerAction.Value);
            }
            if (!computerPlay)
            {
                SelectActivePlayer();
            }
            lastCardPlayed = DateTime.Now;
            UpdateStatus();
        }

        private void ButtonGiveUp_Click(object sender, RoutedEventArgs e)
        {
            var player = GetPlayer();
            if (player == null) return;
            if (skatTable.CanGiveUp(player))
            {
                skatTable.GiveUp();
                showLastStitch = false;
                UpdateStatus();
            }
        }

        private void ButtonViewLastStitch_Click(object sender, RoutedEventArgs e)
        {
            var player = GetPlayer();
            if (player == null) return;
            showLastStitch = !showLastStitch;
            lastCardPlayed = DateTime.Now;
            UpdateStatus();
        }

        private void ButtonComputerPlayer_Click(object sender, RoutedEventArgs e)
        {
            var player = GetPlayer();
            if (player == null) return;
            computerPlay = !computerPlay;
            lastCardPlayed = DateTime.Now;
            UpdateStatus();
        }

        private void ButtonCollectStitch_Click(object sender, RoutedEventArgs e)
        {
            var player = GetPlayer();
            if (player == null) return;
            if (skatTable.CanCollectStitch(player))
            {
                skatTable.CollectStitch(player);
                lastCardPlayed = DateTime.Now;
                UpdateStatus();
            }
        }

        private void ImageSkat_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var image = sender as Image;
            var card = image?.Tag as Card;
            var player = GetPlayer();
            if (player == null || image == null || card == null) return;
            if (skatTable.CanPickupSkat(player))
            {
                skatTable.PickupSkat(player, card);
            }
            lastCardPlayed = DateTime.Now;
            UpdateStatus();
        }

        private void ImagePlayerCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var image = sender as Image;
            var card = image?.Tag as Card;
            var player = GetPlayer();
            if (player == null || image == null || card == null || showLastStitch) return;
            skatTable.PlayCard(player, card);
            if (!computerPlay && player != skatTable.CurrentPlayer)
            {
                SelectActivePlayer();
            }
            lastCardPlayed = DateTime.Now;
            UpdateStatus();
        }

        private void ImageStitch_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var player = GetPlayer();
            if (player == null) return;
            if (skatTable.CanCollectStitch(player))
            {
                skatTable.CollectStitch(player);
                lastCardPlayed = DateTime.Now;
                UpdateStatus();
            }
        }

        private void ImageLastStitch_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var player = GetPlayer();
            if (player == null) return;
            if (showLastStitch)
            {
                showLastStitch = false;
                UpdateStatus();
            }
        }

        private void TextBlockPlayer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var player = GetPlayer();
            var tb = sender as TextBlock;
            if (player == null || sender == null) return;
            if (e.ClickCount == 2)
            {
                selectedPlayer = tb.Tag as Player;
                UpdateStatus();
            }
        }
    }
}
