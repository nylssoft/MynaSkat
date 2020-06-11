using MynaSkat.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
        private Button[] buttonReizenYes;
        private Button[] buttonReizenNo;
        private Image[] imageSkat;
        private Image[] imageCard;
        private Image[] imageStich;
        private Image[] imageOuvert;
        private Image[] imageLastStich;

        public MainWindow()
        {
            InitializeComponent();
            radioButtonPlayer = new RadioButton[] { radioButtonPlayer1, radioButtonPlayer2, radioButtonPlayer3 };
            textBlockStatus = new TextBlock[] { textBlockStatus1, textBlockStatus2, textBlockStatus3 };
            textBlockGame = new TextBlock[] { textBlockGame1, textBlockGame2, textBlockGame3 };
            buttonReizenYes = new Button[] { buttonReizenYes1, buttonReizenYes2, buttonReizenYes3 };
            buttonReizenNo = new Button[] { buttonReizenNo1, buttonReizenNo2, buttonReizenNo3 };
            imageSkat = new Image[] { imageSkat0, imageSkat1 };
            imageCard = new Image[] { imageCard0, imageCard1, imageCard2, imageCard3, imageCard4, imageCard5,
                imageCard6, imageCard7, imageCard8, imageCard9, imageCard10, imageCard11};
            imageStich = new Image[] { imageStich1, imageStich2, imageStich3 };
            imageOuvert = new Image[] { imageOuvert0, imageOuvert1, imageOuvert2, imageOuvert3, imageOuvert4, imageOuvert5,
                imageOuvert6, imageOuvert7, imageOuvert8, imageOuvert9 };
            imageLastStich = new Image[] { imageLastStich0, imageLastStich1, imageLastStich2 };
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
                Card.Sort(player.Cards, player.Game);
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
            Player activePlayer = null;
            foreach (var p in skatTable.Players)
            {
                if (skatTable.GamePlayer == null)
                {
                    if (p.ReizStatus == ReizStatus.Sagen && !skatTable.ReizSaid ||
                        p.ReizStatus == ReizStatus.Hoeren && skatTable.ReizSaid)
                    {
                        activePlayer = p;
                        break;
                    }
                }
                else
                {
                    if (skatTable.CurrentPlayer == null)
                    {
                        activePlayer = skatTable.GamePlayer;
                        break;
                    }
                    activePlayer = skatTable.CurrentPlayer;
                    break;
                }
            }
            var idx = GetPlayerIndex(activePlayer);
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
            Card.Sort(viewPlayer.Cards, viewPlayer.Game);
            UpdatePlayerCards(viewPlayer);
            UpdateOuvertCards(viewPlayer);
            UpdateLastStichCards(viewPlayer);
            // update game type for viewed player
            radionButtonGrand.IsChecked = viewPlayer.Game.Type == GameType.Grand;
            radionButtonNull.IsChecked = viewPlayer.Game.Type == GameType.Null;
            radionButtonKreuz.IsChecked = viewPlayer.Game.Type == GameType.Color && viewPlayer.Game.Color == CardColor.Kreuz;
            radionButtonPik.IsChecked = viewPlayer.Game.Type == GameType.Color && viewPlayer.Game.Color == CardColor.Pik;
            radionButtonHerz.IsChecked = viewPlayer.Game.Type == GameType.Color && viewPlayer.Game.Color == CardColor.Herz;
            radionButtonKaro.IsChecked = viewPlayer.Game.Type == GameType.Color && viewPlayer.Game.Color == CardColor.Karo;
            // update game type for viewed player
            checkBoxHand.IsChecked = viewPlayer.Game.Option.HasFlag(GameOption.Hand);
            checkBoxOuvert.IsChecked = viewPlayer.Game.Option.HasFlag(GameOption.Ouvert);
            checkBoxSchneider.IsChecked = viewPlayer.Game.Option.HasFlag(GameOption.Schneider);
            checkBoxSchwarz.IsChecked = viewPlayer.Game.Option.HasFlag(GameOption.Schwarz);
            // update status and reiz buttons for all players
            int idx = 0;
            foreach (var player in skatTable.Players)
            {
                textBlockStatus[idx].Text = "";
                textBlockGame[idx].Text = "";
                buttonReizenYes[idx].Visibility = Visibility.Hidden;
                buttonReizenNo[idx].Visibility = Visibility.Hidden;
                if (skatTable.GamePlayer == null)
                {
                    if (player.ReizStatus == ReizStatus.Warten)
                    {
                        textBlockStatus[idx].Text = "Wartet.";
                    }
                    else if (player.ReizStatus == ReizStatus.Hoeren && !skatTable.ReizSaid)
                    {
                        textBlockStatus[idx].Text = "Hört auf Reizansage.";
                        if (skatTable.CurrentReizValue > 0)
                        {
                            textBlockStatus[idx].Text += $" {skatTable.CurrentReizValue} angesagt.";
                        }
                    }
                    else if (player.ReizStatus == ReizStatus.Hoeren && skatTable.ReizSaid)
                    {
                        textBlockStatus[idx].Text = "Antworten!";
                        if (viewPlayer == player)
                        {
                            buttonReizenYes[idx].Content = $"{skatTable.CurrentReizValue} halten";
                            buttonReizenYes[idx].Visibility = Visibility.Visible;
                            buttonReizenNo[idx].Content = "Weg";
                            buttonReizenNo[idx].Visibility = Visibility.Visible;
                        }
                    }
                    else if (player.ReizStatus == ReizStatus.Sagen && !skatTable.ReizSaid)
                    {
                        textBlockStatus[idx].Text = "Reizen!";
                        if (viewPlayer == player)
                        {
                            buttonReizenYes[idx].Content = $"{skatTable.NextReizValue} sagen";
                            buttonReizenYes[idx].Visibility = Visibility.Visible;
                            buttonReizenNo[idx].Content = "Weg";
                            buttonReizenNo[idx].Visibility = Visibility.Visible;
                        }
                    }
                    else if (player.ReizStatus == ReizStatus.Sagen && skatTable.ReizSaid)
                    {
                        textBlockStatus[idx].Text = $"Wartet auf Antwort. {skatTable.CurrentReizValue} gesagt.";
                    }
                    else if (player.ReizStatus == ReizStatus.Passen)
                    {
                        textBlockStatus[idx].Text = "Weg.";
                    }
                }
                else if (!skatTable.GameStarted)
                {
                    textBlockStatus[idx].Text = $"Wartet auf Spielansage von {skatTable.GamePlayer.Name}.";
                    if (player == skatTable.GamePlayer)
                    {
                        if (skatTable.Skat.Count < 2)
                        {
                            textBlockStatus[idx].Text = $"Drücken!";
                        }
                        else if (!skatTable.SkatTaken && checkBoxHand.IsChecked == false)
                        {
                            textBlockStatus[idx].Text = $"Skat nehmen oder Hand ansagen!";
                            if (viewPlayer == player)
                            {
                                buttonReizenYes[idx].Content = $"Skat nehmen";
                                buttonReizenYes[idx].Visibility = Visibility.Visible;
                                buttonReizenNo[idx].Content = "Hand spielen";
                                buttonReizenNo[idx].Visibility = Visibility.Visible;
                                textBlockGame[idx].Text += $"Du wirst {viewPlayer.Game.GetGameText()} spielen. ";
                            }
                        }
                        else
                        {
                            textBlockStatus[idx].Text = $"Spiel ansagen oder drücken!";
                            if (viewPlayer == player)
                            {
                                buttonReizenYes[idx].Content = $"Los geht's!";
                                buttonReizenYes[idx].Visibility = Visibility.Visible;
                                if (player.Game.Option.HasFlag(GameOption.Hand))
                                {
                                    buttonReizenNo[idx].Content = $"Kein Handspiel!";
                                    buttonReizenNo[idx].Visibility = Visibility.Visible;
                                }
                                textBlockGame[idx].Text += $"Du wirst {viewPlayer.Game.GetGameText()} spielen. ";
                            }
                        }
                        textBlockStatus[idx].Text += $" Du hast {skatTable.CurrentReizValue} angesagt.";
                    }
                }
                else
                {
                    if (player.Cards.Count == 0 && skatTable.Stich.Count == 0)
                    {
                        textBlockStatus[idx].Text = "Spiel beendet.";
                        List<Card> skat = null;
                        if (player == skatTable.GamePlayer && player.Game.Type != GameType.Null)
                        {
                            skat = skatTable.Skat;
                        }
                        textBlockGame[idx].Text += $"{Card.GetAugen(player.Stiche, skat)} Augen. ";
                        if (player == skatTable.GamePlayer)
                        {
                            textBlockGame[idx].Text += $"{skatTable.Spielwert.Beschreibung} ";
                        }
                    }
                    else
                    {
                        if (player == skatTable.CurrentPlayer)
                        {
                            if (skatTable.Stich.Count == 3)
                            {
                                textBlockStatus[idx].Text = "Stich einsammeln!";
                            }
                            else
                            {
                                textBlockStatus[idx].Text = "Ausspielen!";
                            }
                        }
                        else
                        {
                            textBlockStatus[idx].Text = $"Wartet auf {skatTable.CurrentPlayer.Name}. ";
                        }
                        if (player == skatTable.GamePlayer)
                        {
                            textBlockGame[idx].Text += $"Spielt {viewPlayer.Game.GetGameText()}. ";
                            textBlockGame[idx].Text += $"Hat {skatTable.CurrentReizValue} gesagt. ";
                        }
                    }
                }
                if (player.Position == PlayerPosition.Geben)
                {
                    textBlockStatus[idx].Text += " Hat gegeben.";
                }
                textBlockGame[idx].Text += $"{player.Score} Punkte.";
                idx++;
            }
        }

        private void UpdateControls()
        {
            var player = GetPlayer();
            if (init || player == null) return;
            radionButtonGrand.IsEnabled = !skatTable.GameStarted;
            radionButtonNull.IsEnabled = !skatTable.GameStarted;
            radionButtonKreuz.IsEnabled = !skatTable.GameStarted;
            radionButtonPik.IsEnabled = !skatTable.GameStarted;
            radionButtonHerz.IsEnabled = !skatTable.GameStarted;
            radionButtonKaro.IsEnabled = !skatTable.GameStarted;

            checkBoxOuvert.IsEnabled =
                !skatTable.GameStarted &&
                skatTable.GamePlayer == player &&
                (skatTable.GamePlayer.Game.Type == GameType.Null ||
                 !skatTable.SkatTaken);

            checkBoxHand.IsEnabled =
                !skatTable.GameStarted &&
                !skatTable.SkatTaken &&
                skatTable.GamePlayer == player &&
                (checkBoxOuvert.IsChecked == false ||
                 skatTable.GamePlayer.Game.Type == GameType.Null);

            checkBoxSchneider.IsEnabled =
                checkBoxHand.IsEnabled &&
                skatTable.GamePlayer.Game.Type != GameType.Null &&
                checkBoxHand.IsChecked == true &&
                checkBoxOuvert.IsChecked == false;

            checkBoxSchwarz.IsEnabled = checkBoxSchneider.IsEnabled &&
                checkBoxSchneider.IsChecked == true;

            if (skatTable.GameStarted && skatTable.GamePlayer != null && skatTable.GamePlayer.Cards.Count == 0 && skatTable.Stich.Count == 0)
            {
                buttonNewGame.Visibility = Visibility.Visible;
            }
            else
            {
                buttonNewGame.Visibility = Visibility.Hidden;
            }

            checkBoxLastStich.IsEnabled =
                skatTable.GameStarted &&
                skatTable.LetzterStich.Count > 0 &&
                player == skatTable.CurrentPlayer &&
                player.Cards.Count > 0;
            checkBoxLastStich.Visibility = checkBoxLastStich.IsEnabled ? Visibility.Visible : Visibility.Hidden;
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
                    if (stichIdx < skatTable.Stich.Count)
                    {
                        imageStich[stichIdx].Source = bitmapCache[skatTable.Stich[stichIdx].InternalNumber];
                    }
                    else
                    {
                        imageStich[stichIdx].Source = null;
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

        private Game GetSelectedGame()
        {
            Game game = null;
            if (radionButtonGrand.IsChecked == true)
            {
                game = new Game(GameType.Grand);
            }
            else if (radionButtonNull.IsChecked == true)
            {
                game = new Game(GameType.Null);
            }
            else if (radionButtonKreuz.IsChecked == true)
            {
                game = new Game(GameType.Color, CardColor.Kreuz);
            }
            else if (radionButtonPik.IsChecked == true)
            {
                game = new Game(GameType.Color, CardColor.Pik);
            }
            else if (radionButtonHerz.IsChecked == true)
            {
                game = new Game(GameType.Color, CardColor.Herz);
            }
            else if (radionButtonKaro.IsChecked == true)
            {
                game = new Game(GameType.Color, CardColor.Karo);
            }
            if (game != null)
            {
                game.Option = GameOption.None;
                if (checkBoxOuvert.IsChecked == true)
                {
                    game.Option |= GameOption.Ouvert;
                }
                if (checkBoxHand.IsChecked == true)
                {
                    game.Option |= GameOption.Hand;
                }
                if (checkBoxSchneider.IsChecked == true)
                {
                    game.Option |= GameOption.Schneider;
                }
                if (checkBoxSchwarz.IsChecked == true)
                {
                    game.Option |= GameOption.Schwarz;
                }
            }
            return game;
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
                skatTable.LetzterStich.Count > 0 &&
                player.Cards.Count > 0 &&
                player == skatTable.CurrentPlayer;
            foreach (var img in imageLastStich)
            {
                img.Source =  show ? bitmapBack : null;
            }
            if (show && checkBoxLastStich.IsChecked == true)
            {
                int idx = 0;
                foreach (var card in skatTable.LetzterStich)
                {
                    imageLastStich[idx++].Source = bitmapCache[card.InternalNumber];
                }
            }
        }

        // callbacks

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CreateNewTable();
        }

        private void RadioButtonPlayer_Click(object sender, RoutedEventArgs e)
        {
            var player = GetPlayer();
            if (init || player == null) return;
            checkBoxLastStich.IsChecked = false;
            UpdateStatus();
        }

        private void RadioButtonGameType_Click(object sender, RoutedEventArgs e)
        {
            var player = GetPlayer();
            if (init || player == null) return;
            checkBoxOuvert.IsChecked = false;
            checkBoxHand.IsChecked = false;
            checkBoxSchneider.IsChecked = false;
            checkBoxSchwarz.IsChecked = false;
            checkBoxLastStich.IsChecked = false;
            var game = GetSelectedGame();
            if (game != null)
            {
                player.Game = game;
            }
            UpdateStatus();
        }

        private void CheckBoxOption_Click(object sender, RoutedEventArgs e)
        {
            var player = GetPlayer();
            if (init || player == null) return;
            bool isNull = player.Game?.Type == GameType.Null;
            if (!isNull)
            {
                if (sender == checkBoxOuvert)
                {
                    checkBoxHand.IsChecked = checkBoxOuvert.IsChecked;
                    checkBoxSchneider.IsChecked = checkBoxOuvert.IsChecked;
                    checkBoxSchwarz.IsChecked = checkBoxOuvert.IsChecked;
                }
                if (checkBoxHand.IsChecked == false)
                {
                    checkBoxSchneider.IsChecked = false;
                }
                if (checkBoxSchneider.IsChecked == false)
                {
                    checkBoxSchwarz.IsChecked = false;
                }
            }
            var game = GetSelectedGame();
            if (game != null)
            {
                player.Game = game;
            }
            checkBoxLastStich.IsChecked = false;
            UpdateStatus();
        }

        private void CheckBoxLastStich_Click(object sender, RoutedEventArgs e)
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
                Card.Sort(player.Cards, player.Game);
            }
            imageSkat0.Source = bitmapBack;
            imageSkat1.Source = bitmapBack;
            SelectActivePlayer();
            checkBoxLastStich.IsChecked = false;
            UpdateStatus();
        }

        private void Button_TakeSkat(object sender, RoutedEventArgs e)
        {
            var player = GetPlayer();
            if (init || player == null || skatTable.GameStarted || skatTable.SkatTaken ||
                skatTable.GamePlayer != player) return;
            skatTable.SkatTaken = true;
            UpdateStatus();
        }

        private void Button_StartGame(object sender, RoutedEventArgs e)
        {
            var player = GetPlayer();
            if (init || player != skatTable.GamePlayer ||
                skatTable.Skat.Count != 2 || skatTable.GameStarted) return;
            List<Card> skat = null;
            if (!player.Game.Option.HasFlag(GameOption.Hand))
            {
                skat = skatTable.Skat;
            }
            var spitzen = player.Game.GetSpitzen(player.Cards, skat);
            var reizvalue = player.Game.GetReizWert(spitzen);
            if (reizvalue < skatTable.CurrentReizValue)
            {
                if (MessageBox.Show($"Der Reizwert für das Spiel {skatTable.GamePlayer.Game} ist {reizvalue}. Du hast aber bis {skatTable.CurrentReizValue} gereizt. Willst Du trotzdem spielen?",
                    "Reizen", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    return;
            }
            skatTable.GameStarted = true;
            foreach (var p in skatTable.Players)
            {
                p.Game = player.Game; // same card sort order for everybody
                if (p.Position == PlayerPosition.Hoeren)
                {
                    skatTable.CurrentPlayer = p;
                }
            }
            // spitzen mit skat
            skatTable.Spitzen = player.Game.GetSpitzen(player.Cards, skat);
            SelectActivePlayer();
            checkBoxLastStich.IsChecked = false;
            UpdateStatus();
        }

        private void ButtonReizen_Click(object sender, RoutedEventArgs e)
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
            var isYes = buttonReizenYes.Any((b) => b == button);
            var isNo = buttonReizenNo.Any((b) => b == button);
            // Game actions
            if (skatTable.GamePlayer != null)
            {
                if (!skatTable.SkatTaken && checkBoxHand.IsChecked == false)
                {
                    if (isYes)
                    {
                        Button_TakeSkat(null, null);
                    }
                    else if (isNo)
                    {
                        checkBoxHand.IsChecked = true;
                        CheckBoxOption_Click(null, null);
                    }
                }
                else
                {
                    if (isYes)
                    {
                        Button_StartGame(null, null);
                    }
                    else if (isNo)
                    {
                        checkBoxHand.IsChecked = false;
                        if (checkBoxOuvert.IsChecked == true &&
                            skatTable.GamePlayer.Game.Type != GameType.Null)
                        {
                            checkBoxOuvert.IsChecked = false;
                        }
                        CheckBoxOption_Click(null, null);
                    }
                }
            }
            // Reizen actions
            else
            {
                if (player.ReizStatus == ReizStatus.Sagen && !skatTable.ReizSaid)
                {
                    if (isYes)
                    {
                        skatTable.ReizSaid = true;
                        skatTable.MoveNextReizValue();
                    }
                    else if (isNo)
                    {
                        player.ReizStatus = ReizStatus.Passen;
                        foreach (var p in skatTable.Players)
                        {
                            if (p.Position == PlayerPosition.Geben && p.ReizStatus != ReizStatus.Passen)
                            {
                                p.ReizStatus = ReizStatus.Sagen;
                                break;
                            }
                        }
                        skatTable.ReizSaid = false;
                    }
                }
                else if (player.ReizStatus == ReizStatus.Hoeren)
                {
                    if (isYes)
                    {
                        skatTable.ReizSaid = false;
                    }
                    else if (isNo)
                    {
                        skatTable.ReizSaid = false;
                        player.ReizStatus = ReizStatus.Passen;
                        foreach (var p in skatTable.Players)
                        {
                            if (p.Position == PlayerPosition.Geben && p.ReizStatus != ReizStatus.Passen) // weitersagen
                            {
                                p.ReizStatus = ReizStatus.Sagen;
                            }
                            else if (p.Position == PlayerPosition.Sagen && p.ReizStatus != ReizStatus.Passen) // hoeren
                            {
                                p.ReizStatus = ReizStatus.Hoeren;
                            }
                        }
                    }
                }
                Player gamePlayer = null;
                var cntPassen = 0;
                foreach (var p in skatTable.Players)
                {
                    if (p.ReizStatus != ReizStatus.Passen)
                    {
                        gamePlayer = p;
                        continue;
                    }
                    cntPassen++;
                }
                if (cntPassen == 3)
                {
                    CreateNewTable();
                }
                else if (gamePlayer != null && cntPassen == 2)
                {
                    if (gamePlayer.Position == PlayerPosition.Hoeren && skatTable.CurrentReizValue == 0)
                    {
                        gamePlayer.ReizStatus = ReizStatus.Sagen;
                    }
                    else
                    {
                        skatTable.GamePlayer = gamePlayer;
                        skatTable.GameStarted = false;
                        skatTable.SkatTaken = false;
                    }
                }
            }
            SelectActivePlayer();
            UpdateStatus();
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var player = GetPlayer();
            if (init || player == null) return;
            checkBoxLastStich.IsChecked = false;
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
                        if (skatTable.Stich.Count == 3)
                        {
                            ImageStich_MouseDown(sender, e);
                        }
                        var card = player.Cards[idx];
                        if (!skatTable.IsValidForStich(card))
                        {
                            break;
                        }
                        var playerIdx = GetPlayerIndex(player);
                        player.Cards.RemoveAt(idx);
                        var nextPlayerIdx = (playerIdx + 1) % 3;
                        skatTable.CurrentPlayer = skatTable.Players[nextPlayerIdx];
                        skatTable.Stich.Add(card);
                        if (skatTable.Stich.Count == 3)
                        {
                            var stichPlayer = skatTable.GetStichPlayer();
                            stichPlayer.Stiche.AddRange(skatTable.Stich);
                            skatTable.CurrentPlayer = stichPlayer;
                        }
                        SelectActivePlayer();
                        break;
                    }
                }
            }
            UpdateStatus();
        }

        private void ImageStich_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var player = GetPlayer();
            if (init || player == null) return;
            checkBoxLastStich.IsChecked = false;
            if (skatTable.GameStarted && skatTable.CurrentPlayer == player && skatTable.Stich.Count >= 3)
            {
                skatTable.LetzterStich.Clear();
                skatTable.LetzterStich.AddRange(skatTable.Stich);
                skatTable.Stich.Clear();
            }
            if (player.Cards.Count == 0)
            {
                var game = skatTable.GamePlayer.Game;
                skatTable.Spielwert = game.GetSpielWert(skatTable.Spitzen, skatTable.GamePlayer.Stiche, skatTable.Skat, skatTable.CurrentReizValue);
                skatTable.GamePlayer.Score += skatTable.Spielwert.Punkte;
            }
            UpdateStatus();
        }
    }
}
