# healthcare-ai-chatbot

This repository contains a small backend and Vue frontend that demonstrate a healthcare-focused chatbot.

Overview
- Backend: `HealthcareChatbot.Api` — an ASP.NET Core Web API that forwards user questions to a local Ollama server (`http://localhost:11434`) and returns the model's response.
- Frontend: `vue-project` — a minimal Vue + Vite app that calls the backend API.

Key behavior
- The backend sends every user query to Ollama. Ollama is instructed to act as both a classifier and an assistant and MUST return a single JSON object:

	{
		"isHealthcare": true|false,
		"answer": "..."
	}

- If `isHealthcare` is `true`, the `answer` should contain a helpful, non-diagnostic response and recommend consulting a healthcare professional when appropriate.
- If `isHealthcare` is `false`, the `answer` should be a short refusal message (for example: "This is outside healthcare scope.").

Why Ollama
- Using Ollama locally avoids external API limits and allows running open models like `gemma:2b`.

Requirements
- .NET SDK (tested with .NET 8+)
- Node.js (for the frontend, optional)
- Ollama running locally and accessible at `http://localhost:11434` with the `gemma:2b` model available.

Quick setup

1. Install and run Ollama (instructions vary by OS). Make sure the server is running and the model is available:

	 ```bash
	 ollama pull gemma:2b
	 ollama serve
	 ```

2. Run the backend API

	 ```bash
	 cd HealthcareChatbot.Api
	 dotnet run
	 ```

	 The API will start (by default) on ports such as `http://localhost:5000` and `https://localhost:5001` depending on Kestrel configuration.

3. Run the frontend (optional)

	 ```bash
	 cd vue-project
	 npm install
	 npm run dev
	 ```

Testing the chat endpoint

Use curl or similar to POST a JSON body to the `/chat` endpoint. Example:

```bash
curl -X POST http://localhost:5000/chat \
	-H "Content-Type: application/json" \
	-d '{"question":"If I have a temperature of 104 degree, what should I do?"}'
```

You should receive JSON from the API containing the model's response.

Customization
- If Ollama uses a different endpoint or model name, update `HealthcareChatbot.Api/Services/OllamaClient.cs`.
- The backend sets `temperature = 0.0` for deterministic outputs and asks Ollama to return a strict JSON object — if you want more creative responses, increase the temperature.

Security & public repo notes
- This repo should not contain private API keys. 
- Be cautious exposing medical advice — this project is a demo and should not replace professional medical guidance.

Questions or changes
- If you want the backend to apply additional checks, or to change the JSON contract, edit `OllamaClient.cs` and the `ChatController` accordingly.
