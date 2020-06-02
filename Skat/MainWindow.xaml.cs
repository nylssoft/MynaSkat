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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
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
            bitmapCache = new Dictionary<int, BitmapImage>();
            for (int idx=0; idx <32; idx++)
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
                buttonReizenYes[idx].Visibility = Visibility.Hidden;
                buttonReizenNo[idx].Visibility = Visibility.Hidden;
                textBlockGame[idx].Text = "";
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
                                textBlockGame[idx].Text = $"Wird {viewPlayer.Game} spielen.";
                            }
                        }
                        textBlockStatus[idx].Text += $" Du hast {skatTable.CurrentReizValue} angesagt.";
                    }
                }
                else
                {
                    if (player == skatTable.CurrentPlayer)
                    {
                        textBlockStatus[idx].Text = "Ausspielen!";
                    }
                    else
                    {
                        textBlockStatus[idx].Text = $"Wartet auf {skatTable.CurrentPlayer.Name}.";
                    }
                    if (player == skatTable.GamePlayer)
                    {
                        textBlockGame[idx].Text = $"Spielt {viewPlayer.Game}.";
                    }
                }
                idx++;
            }
        }

        private void UpdateControls()
        {
            var player = GetPlayer();
            if (init || player == null) return;
            /*
            buttonTakeSkat.IsEnabled =
                !skatTable.GameStarted &&
                !skatTable.SkatTaken &&
                skatTable.GamePlayer == player &&
                checkBoxHand.IsChecked == false;
            buttonStartGame.IsEnabled =
                !skatTable.GameStarted &&
                skatTable.GamePlayer == player &&
                skatTable.Skat.Count == 2 &&
                (checkBoxHand.IsChecked == true || skatTable.SkatTaken);
            */
            radionButtonGrand.IsEnabled = !skatTable.GameStarted;
            radionButtonNull.IsEnabled = !skatTable.GameStarted;
            radionButtonKreuz.IsEnabled = !skatTable.GameStarted;
            radionButtonPik.IsEnabled = !skatTable.GameStarted;
            radionButtonHerz.IsEnabled = !skatTable.GameStarted;
            radionButtonKaro.IsEnabled = !skatTable.GameStarted;

            checkBoxHand.IsEnabled =
                !skatTable.GameStarted &&
                !skatTable.SkatTaken &&
                skatTable.GamePlayer == player;

            checkBoxOuvert.IsEnabled =
                !skatTable.GameStarted &&
                skatTable.GamePlayer == player &&
                (skatTable.GamePlayer.Game.Type == GameType.Null ||
                skatTable.GamePlayer.Game.Type == GameType.Grand);

            checkBoxSchneider.IsEnabled =
                checkBoxHand.IsEnabled &&
                skatTable.GamePlayer.Game.Type != GameType.Null &&
                skatTable.GamePlayer.Game.Type != GameType.Null &&
                checkBoxHand.IsChecked == true;

            checkBoxSchwarz.IsEnabled = checkBoxSchneider.IsEnabled && checkBoxSchneider.IsChecked == true;

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
                if (checkBoxHand.IsChecked == true)
                {
                    game.Option |= GameOption.Hand;
                }
                if (checkBoxOuvert.IsChecked == true)
                {
                    game.Option |= GameOption.Ouvert;
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

        // callbacks

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CreateNewTable();
        }

        private void RadioButtonPlayer_Click(object sender, RoutedEventArgs e)
        {
            var player = GetPlayer();
            if (init || player == null) return;
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
            if (checkBoxHand.IsChecked == false)
            {
                checkBoxSchneider.IsChecked = false;
                checkBoxSchwarz.IsChecked = false;
            }
            var game = GetSelectedGame();
            if (game != null)
            {
                player.Game = game;
            }
            UpdateStatus();
        }

        private void Button_NewGame(object sender, RoutedEventArgs e)
        {
            CreateNewTable();
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
            skatTable.GameStarted = true;
            foreach (var p in skatTable.Players)
            {
                p.Game = player.Game; // same card sort order for everybody
                if (p.Position == PlayerPosition.Hoeren)
                {
                    skatTable.CurrentPlayer = p;
                }
            }
            SelectActivePlayer();
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
            if (skatTable.GamePlayer != null )
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
                    var images = new Image[] {
                    imageCard0, imageCard1, imageCard2, imageCard3, imageCard4,
                    imageCard5, imageCard6, imageCard7, imageCard8, imageCard9,
                    imageCard10, imageCard11};
                    for (int idx = 0; idx < images.Length; idx++)
                    {
                        if (images[idx] == image)
                        {
                            var card = player.Cards[idx];
                            player.Cards.RemoveAt(idx);
                            skatTable.Skat.Add(card);
                            break;
                        }
                    }
                }
            }
            UpdateStatus();
        }
    }
}
