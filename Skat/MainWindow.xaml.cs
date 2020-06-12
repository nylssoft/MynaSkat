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
        private Image[] imageSkat;
        private Image[] imageCard;
        private Image[] imageStitch;
        private Image[] imageOuvert;
        private Image[] imageLastStitch;
        private DateTime lastCardPlayed = DateTime.Now;

        public MainWindow()
        {
            InitializeComponent();
            radioButtonPlayer = new RadioButton[] { radioButtonPlayer1, radioButtonPlayer2, radioButtonPlayer3 };
            textBlockStatus = new TextBlock[] { textBlockStatus1, textBlockStatus2, textBlockStatus3 };
            textBlockGame = new TextBlock[] { textBlockGame1, textBlockGame2, textBlockGame3 };
            buttonAction1 = new Button[] { buttonPlayer1Action1, buttonPlayer2Action1, buttonPlayer3Action1 };
            buttonAction2 = new Button[] { buttonPlayer1Action2, buttonPlayer2Action2, buttonPlayer3Action2 };
            imageSkat = new Image[] { imageSkat0, imageSkat1 };
            imageCard = new Image[] { imageCard0, imageCard1, imageCard2, imageCard3, imageCard4, imageCard5,
                imageCard6, imageCard7, imageCard8, imageCard9, imageCard10, imageCard11};
            imageStitch = new Image[] { imageStitch1, imageStitch2, imageStitch3 };
            imageOuvert = new Image[] { imageOuvert0, imageOuvert1, imageOuvert2, imageOuvert3, imageOuvert4, imageOuvert5,
                imageOuvert6, imageOuvert7, imageOuvert8, imageOuvert9 };
            imageLastStitch = new Image[] { imageLastStitch0, imageLastStitch1, imageLastStitch2 };
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
            imageSkat0.Source = bitmapBack;
            imageSkat1.Source = bitmapBack;
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
            UpdatePlayerCards(viewPlayer);
            UpdateOuvertCards(viewPlayer);
            UpdateLastStichCards(viewPlayer);
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

            checkBoxLastStitch.IsEnabled = skatTable.CanViewLastStitch(player);
            checkBoxLastStitch.Visibility = checkBoxLastStitch.IsEnabled ? Visibility.Visible : Visibility.Hidden;

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
        }

        private void UpdatePlayerCards(Player player)
        {
            // skat view
            if (skatTable.SkatTaken)
            {
                imageSkat[0].Source = null;
                imageSkat[1].Source = null;
            }
            else
            {
                imageSkat[0].Source = bitmapBack;
                imageSkat[1].Source = bitmapBack;
            }
            if (player == skatTable.GamePlayer && skatTable.SkatTaken && !skatTable.GameStarted)
            {
                var skatIdx = 0;
                foreach (var card in skatTable.Skat)
                {
                    imageSkat[skatIdx++].Source = bitmapCache[card.InternalNumber];
                }
            }
            if (skatTable.GameStarted)
            {
                gridStich.Visibility = Visibility.Visible;
                gridSkat.Visibility = Visibility.Hidden;
                for (var stichIdx = 0; stichIdx < 3; stichIdx++)
                {
                    if (stichIdx < skatTable.Stitch.Count)
                    {
                        imageStitch[stichIdx].Source = bitmapCache[skatTable.Stitch[stichIdx].InternalNumber];
                    }
                    else
                    {
                        imageStitch[stichIdx].Source = null;
                    }
                }
            }
            else
            {
                gridStich.Visibility = Visibility.Hidden;
                gridSkat.Visibility = Visibility.Visible;
            }
            // card view with 2 extra cards for Skat
            foreach (var image in imageCard)
            {
                image.Source = null;
            }
            var cardIdx = 0;
            foreach (var card in player.Cards)
            {
                imageCard[cardIdx++].Source = bitmapCache[card.InternalNumber];
            }
            UpdateControls();
        }

        private Player GetPlayer()
        {
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

        private void UpdateOuvertCards(Player player)
        {
            foreach (var img in imageOuvert)
            {
                img.Source = null;
            }
            if (skatTable.GameStarted && skatTable.GamePlayer.Game.Option.HasFlag(GameOption.Ouvert) &&
                player != skatTable.GamePlayer)
            {
                int idx = 0;
                foreach (var card in skatTable.GamePlayer.Cards)
                {
                    imageOuvert[idx++].Source = bitmapCache[card.InternalNumber];
                }
            }
        }

        private void UpdateLastStichCards(Player player)
        {
            var show =
                skatTable.GameStarted &&
                skatTable.LastStitch.Count > 0 &&
                player.Cards.Count > 0 &&
                player == skatTable.CurrentPlayer;
            foreach (var img in imageLastStitch)
            {
                img.Source =  show ? bitmapBack : null;
            }
            if (show && checkBoxLastStitch.IsChecked == true)
            {
                int idx = 0;
                foreach (var card in skatTable.LastStitch)
                {
                    imageLastStitch[idx++].Source = bitmapCache[card.InternalNumber];
                }
            }
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
                using (var prng = new RNGCryptoServiceProvider())
                {
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
                                    Image_MouseDown(imageCard[idx], null);
                                    break;
                                }
                                idx++;
                            }
                            break;
                        }
                    }
                }
            }
        }

        private void RadioButtonPlayer_Click(object sender, RoutedEventArgs e)
        {
            var player = GetPlayer();
            if (init || player == null) return;
            checkBoxLastStitch.IsChecked = false;
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
            checkBoxLastStitch.IsChecked = false;
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
            checkBoxLastStitch.IsChecked = false;
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
            imageSkat0.Source = bitmapBack;
            imageSkat1.Source = bitmapBack;
            SelectActivePlayer();
            checkBoxLastStitch.IsChecked = false;
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

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var player = GetPlayer();
            if (init || player == null) return;
            checkBoxLastStitch.IsChecked = false;
            lastCardPlayed = DateTime.Now;
            var image = sender as Image;
            // druecken
            if (skatTable.GamePlayer == player && skatTable.SkatTaken && !skatTable.GameStarted)
            {
                if (image == imageSkat0 || image == imageSkat1)
                {
                    int idx = image == imageSkat0 ? 0 : 1;
                    if (idx < skatTable.Skat.Count)
                    {
                        var card = skatTable.Skat[idx];
                        skatTable.Skat.RemoveAt(idx);
                        player.Cards.Add(card);
                    }
                }
                else if (skatTable.Skat.Count < 2)
                {
                    for (int idx = 0; idx < imageCard.Length; idx++)
                    {
                        if (imageCard[idx] == image)
                        {
                            var card = player.Cards[idx];
                            player.Cards.RemoveAt(idx);
                            skatTable.Skat.Add(card);
                            break;
                        }
                    }
                }
            }
            else if (skatTable.GameStarted && skatTable.CurrentPlayer == player)
            {
                for (int idx = 0; idx < imageCard.Length; idx++)
                {
                    if (imageCard[idx] == image)
                    {
                        if (skatTable.Stitch.Count == 3)
                        {
                            ImageStitch_MouseDown(sender, e);
                            if (player.Cards.Count == 0)
                            {
                                break;
                            }
                        }
                        var card = player.Cards[idx];
                        if (!skatTable.IsValidForStitch(card))
                        {
                            break;
                        }
                        var playerIdx = GetPlayerIndex(player);
                        player.Cards.RemoveAt(idx);
                        var nextPlayerIdx = (playerIdx + 1) % 3;
                        skatTable.CurrentPlayer = skatTable.Players[nextPlayerIdx];
                        skatTable.Stitch.Add(card);
                        if (skatTable.Stitch.Count == 3)
                        {
                            var stichPlayer = skatTable.GetStitchPlayer();
                            stichPlayer.Stitches.AddRange(skatTable.Stitch);
                            skatTable.CurrentPlayer = stichPlayer;
                        }
                        SelectActivePlayer();
                        break;
                    }
                }
            }
            UpdateStatus();
        }

        private void ImageStitch_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var player = GetPlayer();
            if (init || player == null) return;
            checkBoxLastStitch.IsChecked = false;
            if (skatTable.GameStarted && skatTable.CurrentPlayer == player && skatTable.Stitch.Count >= 3)
            {
                skatTable.LastStitch.Clear();
                skatTable.LastStitch.AddRange(skatTable.Stitch);
                skatTable.Stitch.Clear();
            }
            if (skatTable.CurrentPlayer == player && 
                skatTable.GamePlayer == player &&
                player.Game.Type == GameType.Null && player.Stitches.Any())
            {
                foreach (var p in skatTable.Players)
                {
                    p.Cards.Clear();
                }
            }
            if (player.Cards.Count == 0)
            {
                var game = skatTable.GamePlayer.Game;
                skatTable.GameValue = game.GetGameValue(skatTable.MatadorsJackStraight, skatTable.GamePlayer.Stitches, skatTable.Skat, skatTable.CurrentBidValue);
                skatTable.GamePlayer.Score += skatTable.GameValue.Score;
                skatTable.TotalGames += 1;
            }
            UpdateStatus();
        }
    }
}
