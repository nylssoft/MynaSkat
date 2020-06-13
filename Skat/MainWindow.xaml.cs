using MynaSkat.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MynaSkat
{
    public partial class MainWindow : Window
    {
        private SkatTable skatTable;
        private bool init = false;
        private Dictionary<int, BitmapImage> bitmapCache;
        private BitmapImage bitmapBack;
        private RadioButton[] radioButtonPlayer;
        private TextBlock[] textBlockStatus;
        private TextBlock[] textBlockGame;
        private Button[] buttonAction1;
        private Button[] buttonAction2;
        private DateTime lastCardPlayed = DateTime.Now;
        private bool showLastStitch = false;

        public MainWindow()
        {
            InitializeComponent();
            radioButtonPlayer = new RadioButton[] { radioButtonPlayer1, radioButtonPlayer2, radioButtonPlayer3 };
            textBlockStatus = new TextBlock[] { textBlockStatus1, textBlockStatus2, textBlockStatus3 };
            textBlockGame = new TextBlock[] { textBlockGame1, textBlockGame2, textBlockGame3 };
            buttonAction1 = new Button[] { buttonPlayer1Action1, buttonPlayer2Action1, buttonPlayer3Action1 };
            buttonAction2 = new Button[] { buttonPlayer1Action2, buttonPlayer2Action2, buttonPlayer3Action2 };
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

        private void CreateNewTable()
        {
            init = true;
            skatTable = new SkatTable("Niels", "Michael", "Bernhard");
            int idx = 0;
            foreach (var player in skatTable.Players)
            {
                radioButtonPlayer[idx].Content = player.Name;
                idx++;
            }
            init = false;
            SelectActivePlayer();
            UpdateStatus();
        }

        private void SelectActivePlayer()
        {
            var idx = GetPlayerIndex(skatTable.GetActivePlayer());
            if (idx >= 0)
            {
                radioButtonPlayer[idx].IsChecked = true;
            }
        }

        private void UpdateStatus()
        {
            var viewPlayer = GetPlayer();
            if (init || viewPlayer == null) return;
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
            // update status and actions buttons for all players
            int idx = 0;
            foreach (var player in skatTable.Players)
            {
                buttonAction1[idx].Visibility = Visibility.Hidden;
                buttonAction2[idx].Visibility = Visibility.Hidden;
                var playerStatus = skatTable.GetPlayerStatus(viewPlayer, player);
                textBlockStatus[idx].Text = playerStatus.Status;
                textBlockGame[idx].Text = playerStatus.Game;
                if (playerStatus.ActionLabels.Any())
                {
                    buttonAction1[idx].Content = playerStatus.ActionLabels[0];
                    buttonAction1[idx].Visibility = Visibility.Visible;
                    buttonAction1[idx].Tag = playerStatus.Actions[0];
                    if (playerStatus.ActionLabels.Count > 1)
                    {
                        buttonAction2[idx].Content = playerStatus.ActionLabels[1];
                        buttonAction2[idx].Visibility = Visibility.Visible;
                        buttonAction2[idx].Tag = playerStatus.Actions[1];
                    }
                }
                idx++;
            }
        }

        private void UpdateControls()
        {
            var player = GetPlayer();
            if (init || player == null) return;
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

            if (skatTable.GameStarted && skatTable.GamePlayer != null && skatTable.GamePlayer.Cards.Count == 0 && skatTable.Stitch.Count == 0)
            {
                buttonNewGame.Visibility = Visibility.Visible;
            }
            else
            {
                buttonNewGame.Visibility = Visibility.Hidden;
            }
            buttonGiveUp.IsEnabled = skatTable.CanGiveUp(player);
            buttonGiveUp.Visibility = buttonGiveUp.IsEnabled ? Visibility.Visible : Visibility.Hidden;

            checkBoxComputer.IsEnabled =
                !skatTable.GameStarted ||
                player == skatTable.CurrentPlayer && player == skatTable.GamePlayer;
            if (checkBoxComputer.IsChecked == true &&
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
            if (radioButtonPlayer1.IsChecked == true)
            {
                return skatTable.Players[0];
            }
            if (radioButtonPlayer2.IsChecked == true)
            {
                return skatTable.Players[1];
            }
            return skatTable.Players[2];
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

        private int GetPlayerIndex(Player player)
        {
            int idx = 0;
            foreach (var p in skatTable.Players)
            {
                if (player == p)
                {
                    return idx;
                }
                idx++;
            }
            return -1;
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
            if (skatTable == null || player == null) return;
            RenderOuvertCards(player);
            RenderSkat(player);
            RenderLastStitch(player);
            RenderStitch();
            RenderPlayerCards(player);
        }

        private void RenderOuvertCards(Player player)
        {
            List<Card> ouvertCards = null;
            if (skatTable.GameStarted && skatTable.GamePlayer.Game.Option.HasFlag(GameOption.Ouvert) &&
                player != skatTable.GamePlayer)
            {
                ouvertCards = skatTable.GamePlayer.Cards;
            }
            RenderCards(gridOuvertCards, ouvertCards);
        }

        private void RenderStitch()
        {
            RenderCards(gridStitch, skatTable.Stitch, ImageStitch_MouseDown);
        }

        private void RenderLastStitch(Player player)
        {
            List<Card> cards = null;
            if (skatTable.CanViewLastStitch(player))
            {
                cards = skatTable.LastStitch;
            }
            RenderCards(gridLastStitch, cards, ImageLastStitch_MouseDown, !showLastStitch);
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
                    p.Children.Add(image);
                }
                grid.Children.Add(p);
            }
        }

        // callbacks

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CreateNewTable();
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
            if (checkBoxComputer.IsChecked == false ||
                (DateTime.Now - lastCardPlayed).TotalSeconds < 5)
            {
                return;
            }
            var player = GetPlayer();
            if (skatTable.GameStarted && player == skatTable.CurrentPlayer && player != skatTable.GamePlayer &&
                player.Cards.Count > 0)
            {
                var deck = new List<Card>();
                deck.AddRange(player.Cards);
                using var prng = new RNGCryptoServiceProvider();
                while (deck.Count > 0)
                {
                    var card = Card.DrawOne(prng, deck);
                    if (skatTable.IsValidForStitch(card))
                    {
                        int idx = 0;
                        foreach (var c in player.Cards)
                        {
                            if (card == c)
                            {
                                skatTable.PlayCard(player, card);
                                if (player != skatTable.CurrentPlayer)
                                {
                                    SelectActivePlayer();
                                }
                                lastCardPlayed = DateTime.Now;
                                break;
                            }
                            idx++;
                        }
                        break;
                    }
                }
                UpdateStatus();
            }
        }

        private void RadioButtonPlayer_Click(object sender, RoutedEventArgs e)
        {
            var player = GetPlayer();
            if (init || player == null) return;
            showLastStitch = false;
            UpdateStatus();
        }

        private void RadioButtonGameType_Click(object sender, RoutedEventArgs e)
        {
            var player = GetPlayer();
            if (init || player == null || skatTable.GameStarted) return;
            checkBoxOuvert.IsChecked = false;
            checkBoxHand.IsChecked = false;
            checkBoxSchneider.IsChecked = false;
            checkBoxSchwarz.IsChecked = false;
            showLastStitch = false;
            player.Game = new Game(GetGameType(), GetGameColor())
            {
                Option = GetGameOption()
            };
            UpdateStatus();
        }

        private void CheckBoxOption_Click(object sender, RoutedEventArgs e)
        {
            var player = GetPlayer();
            if (init || player == null) return;
            showLastStitch = false;
            skatTable.SetGameOption(player, GetGameOption());
            UpdateStatus();
        }

        private void CheckBoxLastStitch_Click(object sender, RoutedEventArgs e)
        {
            var player = GetPlayer();
            if (init || player == null) return;
            UpdateStatus();
        }

        private void Button_NewGame(object sender, RoutedEventArgs e)
        {
            skatTable.StartNewRound();
            foreach (var player in skatTable.Players)
            {
                player.SortCards();
            }
            SelectActivePlayer();
            showLastStitch = false;
            UpdateStatus();
        }

        private void ButtonAction_Click(object sender, RoutedEventArgs e)
        {
            var player = GetPlayer();
            var button = sender as Button;
            if (init ||
                player == null ||
                button == null ||
                button.Visibility == Visibility.Hidden ||
                skatTable.GamePlayer != null && (skatTable.GamePlayer != player || skatTable.Skat.Count < 2))
            {
                return;
            }
            var playerAction = button.Tag as PlayerAction?;
            if (playerAction != null)
            {
                skatTable.PerformPlayerAction(player, playerAction.Value);
            }
            SelectActivePlayer();
            UpdateStatus();
        }

        private void ButtonGiveUp_Click(object sender, RoutedEventArgs e)
        {
            var player = GetPlayer();
            if (init || player == null) return;
            if (skatTable.CanGiveUp(player))
            {
                skatTable.GiveUp();
                UpdateStatus();
            }
        }

        private void ImageSkat_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var image = sender as Image;
            var card = image?.Tag as Card;
            var player = GetPlayer();
            if (init || player == null || image == null || card == null) return;
            showLastStitch = false;
            lastCardPlayed = DateTime.Now;
            if (skatTable.CanPickupSkat(player))
            {
                skatTable.PickupSkat(player, card);
            }
            UpdateStatus();
        }

        private void ImagePlayerCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var image = sender as Image;
            var card = image?.Tag as Card;
            var player = GetPlayer();
            if (init || player == null || image == null || card == null) return;
            showLastStitch = false;
            lastCardPlayed = DateTime.Now;
            skatTable.PlayCard(player, card);
            if (player != skatTable.CurrentPlayer)
            {
                SelectActivePlayer();
            }
            UpdateStatus();
        }

        private void ImageStitch_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var player = GetPlayer();
            if (init || player == null) return;
            showLastStitch = false;
            skatTable.CollectStitch(player);
            UpdateStatus();
        }
        private void ImageLastStitch_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var player = GetPlayer();
            if (init || player == null) return;
            if (skatTable.CanViewLastStitch(player))
            {
                showLastStitch = !showLastStitch;
            }
            UpdateStatus();
        }
    }
}
