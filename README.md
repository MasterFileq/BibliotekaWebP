# BibliotekaWeb

BibliotekaWeb to aplikacja webowa ASP.NET Core MVC służąca do kompleksowego zarządzania biblioteką. Umożliwia katalogowanie książek, zarządzanie kontami użytkowników (czytelników, bibliotekarzy, administratorów) oraz śledzenie i obsługę procesu wypożyczeń.

## Kluczowe Funkcje

Aplikacja oferuje szeroki zakres funkcjonalności dla różnych ról użytkowników:

* **Zarządzanie Książkami:**
    * Przeglądanie rozbudowanego katalogu książek z opcjami filtrowania (tytuł, autor, ISBN, tematyka, dostępność).
    * Dodawanie nowych książek do systemu.
    * Edycja informacji o istniejących książkach.
    * Usuwanie książek z katalogu (zabezpieczone przed usunięciem wypożyczonych egzemplarzy).
    * Wyświetlanie szczegółowych informacji o każdej książce.
* **Zarządzanie Użytkownikami (Czytelnikami):**
    * Rejestracja nowych kont czytelników przez Administratora.
    * Przeglądanie listy zarejestrowanych czytelników z możliwością filtrowania (email, liczba aktywnych wypożyczeń, suma kar).
    * Edycja danych czytelników (obecnie email, przez Administratora).
    * Usuwanie kont czytelników (przez Administratora, z automatycznym zwrotem aktywnych wypożyczeń i aktualizacją stanu egzemplarzy).
    * Podgląd szczegółów konta czytelnika (przez Administratora).
* **Zarządzanie Wypożyczeniami:**
    * Proces wypożyczania (przypisywania) książek konkretnym czytelnikom.
    * Monitorowanie wszystkich aktywnych wypożyczeń z opcjami filtrowania (tytuł książki, email czytelnika, zakres dat wypożyczenia).
    * Możliwość oznaczenia książki jako zwróconej przez personel (Bibliotekarz, Administrator).
    * Przedłużanie terminu zwrotu wypożyczonej książki:
        * Przez Czytelnika (limitowana liczba przedłużeń).
        * Przez personel (Bibliotekarz, Administrator) bez ograniczeń liczby przedłużeń z poziomu panelu wypożyczeń.
* **Panel Czytelnika:**
    * Dostęp do listy aktualnie wypożyczonych przez siebie książek.
    * Możliwość samodzielnego przedłużenia terminu zwrotu (zgodnie z regulaminem biblioteki, np. jednokrotnie).
* **Statystyki i Raportowanie:**
    * Wizualizacja danych dotyczących wypożyczeń w formie wykresów (np. liczba bieżących wypożyczeń, wypożyczenia w ostatnich 30 dniach) dostępna dla personelu.
* **System Ról i Uprawnień:**
    * **Gość (Niezalogowany):** Dostęp do strony głównej.
    * **Czytelnik:** Przeglądanie katalogu, zarządzanie własnymi wypożyczeniami.
    * **Bibliotekarz:** Funkcje Czytelnika oraz pełne zarządzanie katalogiem książek i wszystkimi wypożyczeniami, przeglądanie listy czytelników.
    * **Administrator:** Pełne uprawnienia Bibliotekarza oraz zarządzanie kontami użytkowników (Czytelników).

## Technologie

* **Framework:** .NET 8.0 (ASP.NET Core MVC)
* **Baza Danych:** MS SQL Server
* **ORM:** Entity Framework Core 8
* **Uwierzytelnianie i Autoryzacja:** ASP.NET Core Identity
* **Frontend:** HTML, CSS, JavaScript, Bootstrap
* **Wizualizacja Danych:** Chart.js (dla wykresów wypożyczeń)

## Struktura Projektu (Główne Komponenty)

* **Controllers:** Odpowiedzialne za logikę biznesową i przepływ danych między modelami a widokami.
    * `HomeController`: Obsługa stron ogólnych (np. strona główna).
    * `KsiazkasController`: Zarządzanie książkami (CRUD, katalog, wypożyczenia od strony użytkownika).
    * `CzytelniksController`: Zarządzanie użytkownikami w roli "Czytelnik" oraz proces przypisywania im książek.
    * `BibliotekarzController`: Panel dla personelu biblioteki, wyświetlanie listy wszystkich wypożyczeń i statystyk.
* **Models:** Reprezentacja danych aplikacji oraz logika walidacji.
    * `Ksiazka.cs`: Definicja encji książki.
    * `Wypozyczenie.cs`: Definicja encji wypożyczenia.
    * `IdentityUser`: Wbudowany model ASP.NET Core Identity, używany jako podstawa dla użytkowników systemu.
    * `Czytelnik.cs`: Dodatkowy model przechowujący specyficzne dane o czytelniku (imię, nazwisko, numer karty), powiązany z `IdentityUser`.
    * ViewModel'e (np. `CzytelnikViewModel`, `WypozyczenieViewModel`, `RegisterViewModel`): Specyficzne modele używane do przekazywania danych do widoków i z formularzy.
* **Views:** Pliki `.cshtml` (Razor) definiujące interfejs użytkownika.
* **Data:**
    * `ApplicationDbContext.cs`: Kontekst bazy danych Entity Framework Core, zarządzający połączeniem i mapowaniem obiektowo-relacyjnym. Zawiera `DbSet` dla `Ksiazka`, `Wypozyczenie`, `Czytelnik` oraz konfigurację Identity.
* **wwwroot:** Statyczne zasoby (CSS, JS, biblioteki).

## Instalacja i Uruchomienie

1.  **Sklonuj repozytorium:**
    Otwórz terminal lub wiersz poleceń i wykonaj:
    ```bash
    git clone [https://github.com/MasterFileq/BibliotekaWebP.git](https://github.com/MasterFileq/BibliotekaWebP.git)
    cd BibliotekaWebP 
    ```
    *(Jeśli nazwa folderu po sklonowaniu to `BibliotekaWeb`, użyj `cd BibliotekaWeb`)*

2.  **Konfiguracja Connection String:**
    * Otwórz plik `appsettings.json` (lub `appsettings.Development.json` dla środowiska deweloperskiego) w głównym katalogu projektu.
    * Znajdź sekcję `ConnectionStrings` i upewnij się, że wartość dla `DefaultConnection` poprawnie wskazuje na Twoją instancję MS SQL Server. Przykładowo:
        ```json
        {
          "ConnectionStrings": {
            "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=BibliotekaWebDB;Trusted_Connection=True;MultipleActiveResultSets=true"
          },
          // ... pozostałe ustawienia
        }
        ```
        *(Dostosuj ten connection string do swojej konfiguracji serwera SQL)*

3.  **Przywróć pakiety NuGet:**
    W terminalu, będąc w głównym katalogu projektu (tam gdzie plik `.csproj`):
    ```bash
    dotnet restore
    ```

4.  **Zastosuj migracje Entity Framework Core (aktualizacja bazy danych):**
    * Upewnij się, że masz zainstalowane narzędzia EF Core. Jeśli nie, zainstaluj je globalnie:
        ```bash
        dotnet tool install --global dotnet-ef
        ```
        Lub jako narzędzie lokalne (wtedy polecenia `dotnet ef` wykonuj jako `dotnet dotnet-ef`):
        ```bash
        dotnet tool install dotnet-ef
        ```
    * W głównym katalogu projektu wykonaj:
        ```bash
        dotnet ef database update
        ```
    * Alternatywnie, jeśli używasz Visual Studio, możesz użyć Konsoli Menedżera Pakietów (Package Manager Console):
        ```powershell
        Update-Database
        ```

5.  **Uruchomienie aplikacji:**
    * Z linii komend, w głównym katalogu projektu:
        ```bash
        dotnet run
        ```
    * Lub uruchom projekt z poziomu Visual Studio (np. przez naciśnięcie przycisku "Play" lub klawisza F5).
    * Aplikacja domyślnie będzie dostępna pod adresem `https://localhost:XXXX` lub `http://localhost:YYYY` (konkretne porty znajdziesz w konsoli po uruchomieniu lub w pliku `launchSettings.json` w folderze `Properties`).

## Preinstalowani Użytkownicy (do celów testowych)

Aby ułatwić używanie aplikacji, przygotowano następujące konta użytkowników:

* **Administrator:**
    * Login: `a@b.c`
    * Hasło: `Aa123456!`
* **Bibliotekarz:**
    * Login: `b@c.d`
    * Hasło: `Aa123456!`
* **Czytelnik:**
    * Login: `c@d.e`
    * Hasło: `Aa123456!`

Zaloguj się używając jednego z powyższych kont, aby przetestować funkcjonalności dostępne dla poszczególnych ról.
Ważne, aby przy konfiguracji projektu do własnych potrzeb zmienić dane do logowania.

---
