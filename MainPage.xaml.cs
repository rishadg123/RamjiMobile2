using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Devices;

namespace RamjiMobile
{
    public partial class MainPage : ContentPage
    {
        private List<Card> deck = new();
        private List<Card> p1FullHand = new();
        private List<Card> p2FullHand = new();
        private List<Card> p3FullHand = new();
        private Card? leftoverCard;

        private List<List<Card>> p1MeldSlots = new() { new(), new(), new(), new(), new() };
        private List<Card> p1Unassigned = new();

        private List<Meld> p1Melds = new();
        private List<Meld> p2Melds = new();
        private List<Meld> p3Melds = new();

        private int p1Chips = 100, p2Chips = 100, p3Chips = 100;
        private int p1Points = 0, p2Points = 0, p3Points = 0;

        private int currentRound = 1;
        private int lastRoundWinner = 0;
        private int faceUpWinner = 0;

        private Card? selectedCard = null;

        public MainPage()
        {
            InitializeComponent();
        }

        #region Dealing & Claiming

        private void BtnDeal_Clicked(object sender, EventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            ResetHand();
            BuildDeck();

            p1FullHand = deck.Take(17).ToList();
            p2FullHand = deck.Skip(17).Take(17).ToList();
            p3FullHand = deck.Skip(34).Take(17).ToList();
            leftoverCard = deck.Last();

            LeftoverCardHost.Content = CreateCardView(leftoverCard, isSmall: true);

            ResolveLeftover();

            p1Unassigned = new List<Card>(p1FullHand.OrderByDescending(c => c.Rank));
            RenderHandTray();
            UpdateMeldSlotsView();

            BtnDeal.IsEnabled = false;
            BtnAutoArrange.IsEnabled = true;
            BtnLock.IsEnabled = true;
            BtnNext.IsEnabled = false;

            TxtStatusBar.Text = "Cards Dealt! Tap HAND to build.";
        }

        private void ResolveLeftover()
        {
            if (leftoverCard == null) return;
            int targetRank = leftoverCard.Rank == 2 ? 14 : 2;
            string targetDesc = targetRank == 14 ? $"Ace of {leftoverCard.Suit}" : $"2 of {leftoverCard.Suit}";

            if (p1FullHand.Any(c => c.Suit == leftoverCard.Suit && c.Rank == targetRank))
            {
                faceUpWinner = 1;
                p1FullHand.Add(leftoverCard);
                TxtLeftoverOwner.Text = "You (holds " + targetDesc + ")";
            }
            else if (p2FullHand.Any(c => c.Suit == leftoverCard.Suit && c.Rank == targetRank))
            {
                faceUpWinner = 2;
                p2FullHand.Add(leftoverCard);
                TxtLeftoverOwner.Text = "Edward (holds " + targetDesc + ")";
            }
            else
            {
                faceUpWinner = 3;
                p3FullHand.Add(leftoverCard);
                TxtLeftoverOwner.Text = "Charlie (holds " + targetDesc + ")";
            }

            lastRoundWinner = faceUpWinner;
            TxtPrompt.Text = $"Player {faceUpWinner} leads R1";
        }

        #endregion

        #region Touch Interactions & Hand Drawer

        private void BtnHandToggle_Clicked(object sender, EventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            HandDrawer.IsVisible = !HandDrawer.IsVisible;
        }

        private void Card_Tapped(Card card)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            selectedCard = card;
            TxtStatusBar.Text = $"Selected: {card.DisplayText}. Tap a Meld Slot (M1..M5).";
            RenderHandTray();
        }

        private async void MeldSlot_Tapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is string param && int.TryParse(param, out int slotIdx))
            {
                if (selectedCard != null)
                {
                    if (p1MeldSlots[slotIdx].Count >= 3)
                    {
                        await DisplayAlertAsync("Slot Full", $"Meld {slotIdx + 1} already has 3 cards.", "OK");
                        return;
                    }

                    p1Unassigned.Remove(selectedCard);
                    foreach (var slot in p1MeldSlots) slot.Remove(selectedCard);

                    p1MeldSlots[slotIdx].Add(selectedCard);
                    selectedCard = null;

                    HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                    RenderHandTray();
                    UpdateMeldSlotsView();
                }
                else
                {
                    if (p1MeldSlots[slotIdx].Count > 0)
                    {
                        var returned = p1MeldSlots[slotIdx].Last();
                        p1MeldSlots[slotIdx].Remove(returned);
                        p1Unassigned.Add(returned);

                        HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                        RenderHandTray();
                        UpdateMeldSlotsView();
                    }
                }
            }
        }

        private void BtnSortRank_Clicked(object sender, EventArgs e)
        {
            p1Unassigned = p1Unassigned.OrderByDescending(c => c.Rank).ThenBy(c => c.Suit).ToList();
            RenderHandTray();
        }

        private void BtnSortSuit_Clicked(object sender, EventArgs e)
        {
            p1Unassigned = p1Unassigned.OrderBy(c => c.Suit).ThenByDescending(c => c.Rank).ToList();
            RenderHandTray();
        }

        private void RenderHandTray()
        {
            HandTrayStack.Children.Clear();
            foreach (var card in p1Unassigned)
            {
                bool isSelected = (selectedCard == card);
                var view = CreateCardView(card, isSelected: isSelected);
                view.GestureRecognizers.Add(new TapGestureRecognizer
                {
                    Command = new Command(() => Card_Tapped(card))
                });
                HandTrayStack.Children.Add(view);
            }
            BtnHandToggle.Text = $"HAND ({p1Unassigned.Count})";
        }

        private void UpdateMeldSlotsView()
        {
            UpdateSingleSlot(MeldHost0, MeldLbl0, p1MeldSlots[0]);
            UpdateSingleSlot(MeldHost1, MeldLbl1, p1MeldSlots[1]);
            UpdateSingleSlot(MeldHost2, MeldLbl2, p1MeldSlots[2]);
            UpdateSingleSlot(MeldHost3, MeldLbl3, p1MeldSlots[3]);
            UpdateSingleSlot(MeldHost4, MeldLbl4, p1MeldSlots[4]);
        }

        private void UpdateSingleSlot(HorizontalStackLayout host, Label label, List<Card> cards)
        {
            host.Children.Clear();
            for (int i = 0; i < cards.Count; i++)
            {
                var cardView = CreateCardView(cards[i], isOverlap: (i > 0), isSmall: true);
                host.Children.Add(cardView);
            }

            if (cards.Count == 3)
            {
                var meld = MeldEvaluator.Evaluate(cards);
                label.Text = meld.DisplayName;
                label.TextColor = meld.Category != MeldCategory.Invalid ? Colors.Gold : Colors.Salmon;
            }
            else
            {
                label.Text = $"{cards.Count}/3";
                label.TextColor = Colors.LightGray;
            }
        }

        #endregion

        #region Lock & AI

        private void BtnAutoArrange_Clicked(object sender, EventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            var (best, discards) = MeldEvaluator.OptimizeHand(p1FullHand);
            p1MeldSlots = new List<List<Card>>();
            for (int i = 0; i < 5; i++)
            {
                p1MeldSlots.Add(i < best.Count ? new List<Card>(best[i].Cards) : new List<Card>());
            }
            p1Unassigned = new List<Card>(discards);

            RenderHandTray();
            UpdateMeldSlotsView();
            TxtStatusBar.Text = "Optimal Melds Set! Tap LOCK.";
        }

        private async void BtnLock_Clicked(object sender, EventArgs e)
        {
            for (int i = 0; i < 5; i++)
            {
                if (p1MeldSlots[i].Count != 3)
                {
                    await DisplayAlertAsync("Incomplete", $"Meld {i + 1} must have 3 cards.", "OK");
                    return;
                }
            }

            var evaluated = new List<Meld>();
            for (int i = 0; i < 5; i++)
            {
                var m = MeldEvaluator.Evaluate(p1MeldSlots[i]);
                if (m.Category == MeldCategory.Invalid)
                {
                    await DisplayAlertAsync("Invalid", $"Meld {i + 1} is invalid.", "OK");
                    return;
                }
                evaluated.Add(m);
            }

            for (int i = 0; i < 4; i++)
            {
                if (evaluated[i].CompareTo(evaluated[i + 1]) < 0)
                {
                    await DisplayAlertAsync("Foul Order", $"Meld {i + 1} is weaker than Meld {i + 2}. Must order highest to lowest.", "OK");
                    return;
                }
            }

            p1Melds = evaluated;
            (p2Melds, _) = MeldEvaluator.OptimizeHand(p2FullHand);
            (p3Melds, _) = MeldEvaluator.OptimizeHand(p3FullHand);

            HandDrawer.IsVisible = false;
            BtnLock.IsEnabled = false;
            BtnAutoArrange.IsEnabled = false;
            BtnNext.IsEnabled = true;

            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            TxtStatusBar.Text = "Locked! Tap SHOWDOWN.";
        }

        #endregion

        #region Showdown & Scoring

        private async void BtnNext_Clicked(object sender, EventArgs e)
        {
            if (currentRound > 5) return;
            BtnNext.IsEnabled = false;

            int roundPoints = currentRound;
            int idx = currentRound - 1;

            List<int> turnOrder = lastRoundWinner switch
            {
                1 => new() { 1, 2, 3 },
                2 => new() { 2, 3, 1 },
                _ => new() { 3, 1, 2 }
            };

            Meld m1 = p1Melds[idx];
            Meld m2 = p2Melds[idx];
            Meld m3 = p3Melds[idx];
            var dict = new Dictionary<int, Meld> { { 1, m1 }, { 2, m2 }, { 3, m3 } };

            foreach (var p in turnOrder)
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                if (p == 1) RenderShowMeld(P1ShowCards, P1ShowLabel, m1);
                else if (p == 2) RenderShowMeld(P2ShowCards, P2ShowLabel, m2);
                else RenderShowMeld(P3ShowCards, P3ShowLabel, m3);
                await Task.Delay(400);
            }

            int bestPlayer = turnOrder[0];
            Meld bestMeld = dict[bestPlayer];

            for (int i = 1; i < turnOrder.Count; i++)
            {
                int challenger = turnOrder[i];
                if (dict[challenger].CompareTo(bestMeld) >= 0)
                {
                    bestMeld = dict[challenger];
                    bestPlayer = challenger;
                }
            }

            if (bestPlayer == 1) p1Points += roundPoints;
            else if (bestPlayer == 2) p2Points += roundPoints;
            else p3Points += roundPoints;

            lastRoundWinner = bestPlayer;
            string winnerName = bestPlayer == 1 ? "You" : bestPlayer == 2 ? "Edward" : "Charlie";
            TxtStatusBar.Text = $"{winnerName} won R{currentRound} (+{roundPoints}pt)";

            TxtP1Round.Text = $"{p1Points} pts";
            TxtP2Round.Text = $"{p2Points} pts";
            TxtP3Round.Text = $"{p3Points} pts";

            currentRound++;
            if (currentRound <= 5)
            {
                BtnNext.IsEnabled = true;
            }
            else
            {
                await EndHandAsync();
            }
        }

        private async Task EndHandAsync()
        {
            int diff1 = p1Points - 5;
            int diff2 = p2Points - 5;
            int diff3 = p3Points - 5;

            if (p1Points == 15) { diff1 += 10; diff2 -= 5; diff3 -= 5; }
            else if (p2Points == 15) { diff2 += 10; diff1 -= 5; diff3 -= 5; }
            else if (p3Points == 15) { diff3 += 10; diff1 -= 5; diff2 -= 5; }

            p1Chips += diff1;
            p2Chips += diff2;
            p3Chips += diff3;

            TxtP1Chips.Text = p1Chips.ToString();
            TxtP2Chips.Text = p2Chips.ToString();
            TxtP3Chips.Text = p3Chips.ToString();

            await DisplayAlertAsync("Hand Complete", $"Settlement:\nYou: {(diff1 >= 0 ? "+" : "")}{diff1}\nEdward: {(diff2 >= 0 ? "+" : "")}{diff2}\nCharlie: {(diff3 >= 0 ? "+" : "")}{diff3}", "OK");

            BtnNext.IsEnabled = false;
            BtnDeal.IsEnabled = true;
            TxtStatusBar.Text = "Hand Finished! Tap DEAL.";
        }

        private void RenderShowMeld(HorizontalStackLayout host, Label label, Meld meld)
        {
            host.Children.Clear();
            for (int i = 0; i < meld.Cards.Count; i++)
            {
                host.Children.Add(CreateCardView(meld.Cards[i], isOverlap: (i > 0), isSmall: true));
            }
            label.Text = meld.DisplayName;
        }

        private void ResetHand()
        {
            currentRound = 1;
            p1Points = p2Points = p3Points = 0;
            TxtP1Round.Text = "0 pts";
            TxtP2Round.Text = "0 pts";
            TxtP3Round.Text = "0 pts";
            p1MeldSlots = new() { new(), new(), new(), new(), new() };
            p1Unassigned.Clear();
            P1ShowCards.Children.Clear();
            P2ShowCards.Children.Clear();
            P3ShowCards.Children.Clear();
            P1ShowLabel.Text = "-";
            P2ShowLabel.Text = "-";
            P3ShowLabel.Text = "-";
            LeftoverCardHost.Content = null;
        }

        private void BuildDeck()
        {
            deck.Clear();
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                for (int rank = 2; rank <= 14; rank++)
                {
                    deck.Add(new Card(suit, rank));
                }
            }
            Random rng = new();
            deck = deck.OrderBy(_ => rng.Next()).ToList();
        }

        #endregion

        #region UI Card Component Factory

        private Border CreateCardView(Card card, bool isOverlap = false, bool isSmall = false, bool isSelected = false)
        {
            double width = isSmall ? 36 : 46;
            double height = isSmall ? 52 : 68;

            var border = new Border
            {
                WidthRequest = width,
                HeightRequest = height,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(4) },
                Padding = new Thickness(2),
                BackgroundColor = Colors.White,
                Stroke = isSelected ? Colors.Yellow : (card.IsRed ? Colors.Crimson : Colors.DimGray),
                StrokeThickness = isSelected ? 2 : 1,
                Margin = isOverlap ? new Thickness(-18, 0, 0, 0) : new Thickness(1, 0)
            };

            var grid = new Grid
            {
                RowDefinitions = new RowDefinitionCollection
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Star }
                }
            };

            var topLabel = new Label
            {
                Text = card.RankString,
                FontSize = isSmall ? 10 : 13,
                FontAttributes = FontAttributes.Bold,
                TextColor = card.IsRed ? Colors.Crimson : Colors.Black,
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(1, -1, 0, 0)
            };

            var suitLabel = new Label
            {
                Text = card.SuitSymbol,
                FontSize = isSmall ? 14 : 20,
                TextColor = card.IsRed ? Colors.Crimson : Colors.Black,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            grid.Add(topLabel);
            Grid.SetRow(topLabel, 0);

            grid.Add(suitLabel);
            Grid.SetRow(suitLabel, 1);

            border.Content = grid;
            return border;
        }

        #endregion
    }
}