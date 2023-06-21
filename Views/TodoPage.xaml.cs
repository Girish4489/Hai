using Hai.Data;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;

namespace Hai.Views
{
    public partial class TodoPage : ContentPage
    {
        readonly TodoItemDatabase database;
        TodoItem Item { get; set; }
        public ObservableCollection<TodoItem> TodoItemsCollection { get; set; }

        public TodoPage(TodoItemDatabase todoItemDatabase)
        {
            InitializeComponent();
            BindingContext = this;
            database = todoItemDatabase;
            Item = new TodoItem();
            TodoItemsCollection = new ObservableCollection<TodoItem>();
            _ = LoadTodoItems();
        }

        private async void OnSaveClickedAsync(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(NameEntry.Text))
            {
                await DisplayAlert("Name Required", "Please enter a name for the todo item.", "OK");
                return;
            }

            Item.Name = NameEntry.Text;
            await database.SaveItemAsync(Item);
            await LoadTodoItems();
            NameEntry.Text = string.Empty;
            await Shell.Current.GoToAsync("..");
        }

        private async void OnAddClickedAsync(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(NameEntry.Text))
            {
                await DisplayAlert("Name Required", "Please enter a name for the todo item.", "OK");
                return;
            }

            TodoItem newItem = new()
            {
                Name = NameEntry.Text,
                Done = false
            };
            NameEntry.Text = string.Empty;
            await database.SaveItemAsync(newItem);
            await LoadTodoItems();
        }

        private void AdjustButton(ImageButton button)
        {
            button.WidthRequest = 20;
            button.HeightRequest = 20;
            button.BackgroundColor = Color.FromRgb(20, 189, 173);
            button.CornerRadius = 12;
            button.Margin = new Thickness(5, 0);
        }

        async Task LoadTodoItems()
        {
            var items = await database.GetItemsAsync();
            if (items.Count == 0)
            {
                await database.ResetIDCounter();
            }

            TodoItemsCollection.Clear();
            TodoStack.Children.Clear();

            foreach (var item in items)
            {
                TodoItemsCollection.Add(item);

                var stackLayout = new StackLayout
                {
                    Margin = new Thickness(5, 0),
                    Padding = new Thickness(0, 5),
                    Orientation = StackOrientation.Horizontal,
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    BindingContext = item
                };

                var idLabel = new Label
                {
                    IsVisible = false,
                    Text = item.ID.ToString(),
                    Margin = new Thickness(5, 0)
                };

                var nameEditor = new Editor
                {
                    Text = item.Name,
                    AutoSize = EditorAutoSizeOption.TextChanges,
                    IsReadOnly = true,
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    Margin = new Thickness(5, 0)
                };

                var saveButton = new ImageButton
                {
                    IsVisible = false,
                    Source = "save_as.png",
                };
                AdjustButton(saveButton);
                saveButton.Clicked += OnEditSaveClickedAsync;

                var editButton = new ImageButton
                {
                    Source = "edit.png",
                };
                AdjustButton(editButton);
                editButton.Clicked += OnEditClicked;

                var deleteButton = new ImageButton
                {
                    Source = "delete.png",
                };
                AdjustButton(deleteButton);
                deleteButton.Clicked += OnDeleteClickedAsync;

                stackLayout.Children.Add(idLabel);
                stackLayout.Children.Add(nameEditor);
                stackLayout.Children.Add(editButton);
                stackLayout.Children.Add(saveButton);
                stackLayout.Children.Add(deleteButton);
                TodoStack.Children.Add(stackLayout);
            }

            if (items.Count == 0)
            {
                await database.ResetIDCounter();
            }
        }

        private async void OnDeleteClickedAsync(object sender, EventArgs e)
        {
            if (sender is ImageButton deleteButton && deleteButton.BindingContext is TodoItem selectedTodoItem)
            {
                await database.DeleteItemAsync(selectedTodoItem);
                _ = TodoItemsCollection.Remove(selectedTodoItem);

                var stackLayout = FindParentStackLayout(deleteButton);
                if (stackLayout != null)
                {
                    _ = TodoStack.Children.Remove(stackLayout);
                }
                await LoadTodoItems();
            }
        }

        private async void OnEditClicked(object sender, EventArgs e)
        {
            if (sender is ImageButton editButton && editButton.BindingContext is TodoItem)
            {
                await ToggleEditModeAsync(editButton);
            }
        }

        private async void OnEditSaveClickedAsync(object sender, EventArgs e)
        {
            if (sender is ImageButton saveButton && saveButton.BindingContext is TodoItem editedTodoItem)
            {
                // Save the edited text
                var stackLayout = FindParentStackLayout(saveButton);
                if (stackLayout != null)
                {
                    var nameEditor = stackLayout.Children.OfType<Editor>().FirstOrDefault();
                    if (nameEditor != null)
                    {
                        editedTodoItem.Name = nameEditor.Text;
                        await database.SaveItemAsync(editedTodoItem);
                        await ToggleEditModeAsync(saveButton);
                    }
                }
            }
        }

        private async Task ToggleEditModeAsync(ImageButton editButton)
        {
            var stackLayout = FindParentStackLayout(editButton);
            if (stackLayout != null)
            {
                var nameEditor = stackLayout.Children.OfType<Editor>().FirstOrDefault();
                var saveButton = stackLayout.Children.OfType<ImageButton>().FirstOrDefault(b => (b.Source as FileImageSource)?.File == "save_as.png");

                if (nameEditor != null && saveButton != null)
                {
                    bool isEditing = !nameEditor.IsReadOnly;

                    if (isEditing)
                    {
                        // Save the edited text
                        var editedTodoItem = (TodoItem)stackLayout.BindingContext;
                        nameEditor.IsReadOnly = true;
                        editButton.IsVisible = false;
                        saveButton.IsVisible = false;

                        // Update the database
                        editedTodoItem.Name = nameEditor.Text;
                        await database.SaveItemAsync(editedTodoItem);

                        // Reload the data
                        await LoadTodoItems();
                    }
                    else
                    {
                        // Enter edit mode
                        nameEditor.IsReadOnly = false;
                        editButton.IsVisible = false;
                        saveButton.IsVisible = true;
                    }
                }
            }
        }

        static StackLayout FindParentStackLayout(View view)
        {
            Element parent = view?.Parent;
            while (parent != null)
            {
                if (parent is StackLayout stackLayout)
                {
                    return stackLayout;
                }
                parent = parent.Parent;
            }
            return null;
        }
    }
}
