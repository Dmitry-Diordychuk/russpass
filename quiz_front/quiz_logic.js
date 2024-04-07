const back_address = "http://localhost:5054";

document.getElementById('startQuizBtn').addEventListener('click', function() {
    startQuiz();
});

function startQuiz() {
    fetch(`${back_address}/api/GameSession/start`, { method: 'POST' }) // Предполагается, что метод POST используется для создания сессии
        .then(response => response.json())
        .then(session => {
            console.log("Session start:", session);
            sessionStorage.setItem('sessionId', session.id);

            displaySessionInfo(session);
            session.currentQuestionIds.forEach(questionId => {
                fetchQuestion(questionId);
            });
        })
        .catch(error => console.error('Error starting quiz:', error));
}

function fetchQuestion(questionId) {
    fetch(`${back_address}/api/Questions/${questionId}`)
        .then(response => response.json())
        .then(question => {
            displayQuestion(question);
        })
        .catch(error => console.error('Error fetching question:', error));
}

function displaySessionInfo(session) {
    const quizDiv = document.getElementById('quiz');
    quizDiv.innerHTML = `
    <div>
        <div className="text_with_level">
            <p>
                Уровень ${session.currentLevel}
            </p>
        </div>
    </div>
    `
}

function displayQuestion(question) {
    console.log("question", question);
    const quizDiv = document.getElementById('quiz');
    const questionElement = document.createElement('div');
    questionElement.innerHTML = `
    <div class="first_que">
        <p class="answer_1">${question.text}</p>
        <img class="picture_for_first_question" src="${question.imageUrl}" alt="Купюра">
        ${question.answers.map(answer => `
        <p style="text-align: center">
            <button onclick="answerQuestion(${question.id}, ${answer.id})">${answer.text}</button>
        </p>
        `).join('')}
    </div>
    `;
    quizDiv.appendChild(questionElement);
}

function answerQuestion(questionId, answerId) {
    // Получаем sessionId из sessionStorage
    const sessionId = sessionStorage.getItem('sessionId');
    console.log("Get sessionId", sessionId);

    // Создаем URL с query параметрами
    const url = new URL(`${back_address}/api/GameSession/answer`);
    const params = { sessionId, questionId, answerId };
    url.search = new URLSearchParams(params).toString();

    fetch(url, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
    })
        .then(response => {
            if (!response.ok) {
                // Обработка сетевой ошибки или ошибки сервера
                return response.text().then(text => { throw new Error(text) });
            }
            return response.json(); // Разбор тела ответа как JSON
        })
        .then(result => {
            console.log("Body", result);
            // Если ответ правильный
            if (result.isCorrect) {
                if (result.isComplete) {
                    // Игра завершена
                    document.getElementById('quiz').innerHTML = '<p>Поздравляем! Квиз пройден.</p>';
                } else if (result.goNextLevel) {
                    alert('Правильный ответ! Переход на следующий уровень.');
                    loadQuestions(sessionId); // Здесь функция для загрузки вопросов следующего уровня
                } else {
                    // Продолжаем текущий уровень
                    alert('Правильный ответ! Следующий вопрос.');
                }
            } else {
                alert('Ответ не правильный\n' + result.message);
                loadQuestions(sessionId); // Перезагрузка вопросов
            }
        })
        .catch(error => {
            // Обработка ошибок, включая показ сообщений об ошибке от сервера
            console.error('Error:', error);
            alert(error.message);
        });
}

function loadQuestions(sessionId) {
    fetch(`${back_address}/api/GameSession/state/${sessionId}`, { method: 'GET' })
        .then(response => response.json())
        .then(session => {
            console.log("Session state:", session);

            displaySessionInfo(session);
            session.currentQuestions.forEach(questionId => {
                fetchQuestion(questionId);
            });
        })
        .catch(error => console.error('Error starting quiz:', error));
}
