let connection = new signalR.HubConnectionBuilder()
    .withUrl("/gamehub")
    .withAutomaticReconnect()
    .build();

let currentPlayerId = "";
let currentPlayerName = "";
let currentDisplayName = "";
let currentGameSession = null;
let isPlayerX = false;

connection.on("PlayerRegistered", (data) => {
    currentPlayerId = data.id;
    currentPlayerName = data.name;
    currentDisplayName = data.displayName;

    document.getElementById("nameEntry").classList.add("d-none");
    document.getElementById("playerInfo").classList.remove("d-none");
    document.getElementById("gameControls").classList.remove("d-none");
    document.getElementById("playerNameDisplay").textContent = currentDisplayName;

    fetch(`/Home/SetPlayerName`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name: currentPlayerName })
    });

    loadPlayerStats();
    loadAvailableSessions();
});

connection.on("GameSessionCreated", (session) => {
    currentGameSession = session;
    isPlayerX = true;
    updateGameUI(session);
});

connection.on("AvailableSessionsUpdated", (sessions) => {
    updateAvailableSessionsList(sessions);
});

connection.on("GameStarted", (session) => {
    currentGameSession = session;
    isPlayerX = currentPlayerId === session.playerXId;
    updateGameUI(session);
});

connection.on("MoveMade", (session) => {
    currentGameSession = session;
    updateGameBoard(session);
    updateGameStatus(session);
});

connection.on("JoinFailed", (message) => {
    alert(message);
});

connection.on("OperationFailed", (message) => {
    alert(message);
});

async function registerPlayer() {
    const name = document.getElementById("playerName").value.trim();
    if (!name) return alert("Please enter your name");
    if (name.length > 20) return alert("Name must be 20 characters or less");

    try {
        await connection.invoke("RegisterPlayer", name);
    } catch (err) {
        console.error(err);
        alert("Failed to register. Please try again.");
    }
}

async function createGameSession() {
    const sessionName = document.getElementById("sessionName").value.trim();
    if (!sessionName) return alert("Please enter a session name");
    if (sessionName.length > 30) return alert("Session name must be 30 characters or less");

    try {
        await connection.invoke("CreateGameSession", sessionName, currentPlayerId);
        document.getElementById("sessionName").value = "";
    } catch (err) {
        console.error(err);
        alert("Failed to create game session");
    }
}

async function loadAvailableSessions() {
    try {
        const sessions = await connection.invoke("GetAvailableSessions");
        updateAvailableSessionsList(sessions);
    } catch (err) {
        console.error(err);
    }
}

function updateAvailableSessionsList(sessions) {
    const container = document.getElementById("availableSessions");
    container.innerHTML = "";

    if (sessions.length === 0) {
        container.innerHTML = '<div class="text-center text-muted py-3">No available games</div>';
        return;
    }

    sessions.forEach(session => {
        const sessionElement = document.createElement("div");
        sessionElement.className = "session-item";
        sessionElement.innerHTML = `
            <div class="d-flex justify-content-between align-items-center">
                <div>
                    <strong>${session.sessionName}</strong>
                    <div class="session-creator">By: ${session.creatorName}</div>
                </div>
                <button class="btn btn-sm btn-outline-red" onclick="joinSession('${session.id}')">
                    Join
                </button>
            </div>
            <div class="session-time">Created: ${formatTime(session.createdAt)}</div>
        `;
        container.appendChild(sessionElement);
    });
}

async function joinSession(sessionId) {
    try {
        await connection.invoke("JoinGameSession", sessionId, currentPlayerId);
    } catch (err) {
        console.error(err);
        alert("Failed to join game session");
    }
}

function updateGameUI(session) {
    document.getElementById("waitingArea").classList.add("d-none");
    document.getElementById("gameArea").classList.remove("d-none");
    document.getElementById("gameResult").classList.add("d-none");

    updateGameBoard(session);
    updateGameStatus(session);
}

function updateGameBoard(session) {
    const board = document.getElementById("gameBoard");
    board.innerHTML = "";

    for (let i = 0; i < 9; i++) {
        const cell = document.createElement("div");
        cell.className = "cell";

        if (session.board[i]) {
            cell.textContent = session.board[i];
            cell.classList.add(session.board[i].toLowerCase(), "disabled");
        } else if (session.status === 1 && session.currentPlayerId === currentPlayerId) {
            cell.addEventListener("click", () => makeMove(i));
        } else {
            cell.classList.add("disabled");
        }

        board.appendChild(cell);
    }
}

function updateGameStatus(session) {
    const gameStatus = document.getElementById("gameStatus");
    const currentTurn = document.getElementById("currentTurn");

    if (session.status === 0) {
        gameStatus.textContent = "Waiting for opponent...";
        currentTurn.classList.add("d-none");
    } else if (session.status === 1) {
        const isMyTurn = session.currentPlayerId === currentPlayerId;
        gameStatus.textContent = isMyTurn ? "Your Turn!" : "Opponent's Turn";
        currentTurn.textContent = isMyTurn ?
            `You are playing as ${isPlayerX ? 'X' : 'O'}` :
            `Waiting for opponent's move`;
        currentTurn.classList.remove("d-none");
    } else if (session.status === 2) {
        currentTurn.classList.add("d-none");

        if (session.winner === "Draw") {
            gameStatus.textContent = "Game Over - It's a Draw!";
            showGameResult("It's a Draw!");
        } else {
            const isWinner = session.winner === currentPlayerId;
            gameStatus.textContent = isWinner ? "You Win!" : "You Lose!";
            showGameResult(isWinner ? "You Win!" : "You Lose!");
        }

        loadPlayerStats();
    }
}

async function makeMove(position) {
    try {
        await connection.invoke("MakeMove", currentGameSession.id, currentPlayerId, position);
    } catch (err) {
        console.error(err);
        alert("Invalid move");
    }
}

function showGameResult(message) {
    document.getElementById("resultMessage").textContent = message;
    document.getElementById("gameResult").classList.remove("d-none");
}

function leaveGame() {
    currentGameSession = null;
    document.getElementById("gameArea").classList.add("d-none");
    document.getElementById("waitingArea").classList.remove("d-none");
    document.getElementById("gameStatus").textContent = "Waiting for game...";
}

async function loadPlayerStats() {
    try {
        const response = await fetch(`/Home/GetPlayerStats?playerId=${currentPlayerId}`);
        const stats = await response.json();

        document.querySelector(".stat-number.wins").textContent = stats.wins || 0;
        document.querySelector(".stat-number.losses").textContent = stats.losses || 0;
        document.querySelector(".stat-number.draws").textContent = stats.draws || 0;
    } catch (err) {
        console.error(err);
    }
}

function formatTime(dateString) {
    const date = new Date(dateString);
    const now = new Date();
    const diffMins = Math.floor((now - date) / 60000);

    if (diffMins < 1) return "Just now";
    if (diffMins < 60) return `${diffMins} min ago`;
    const diffHours = Math.floor(diffMins / 60);
    if (diffHours < 24) return `${diffHours} hour${diffHours > 1 ? 's' : ''} ago`;
    return date.toLocaleDateString();
}

async function start() {
    try {
        await connection.start();
        const response = await fetch('/Home/GetPlayerName');
        const savedName = await response.text();
        if (savedName) {
            document.getElementById("playerName").value = savedName;
            await registerPlayer();
        }
    } catch (err) {
        console.error(err);
        setTimeout(start, 5000);
    }
}

start();

window.registerPlayer = registerPlayer;
window.createGameSession = createGameSession;
window.joinSession = joinSession;
window.leaveGame = leaveGame;