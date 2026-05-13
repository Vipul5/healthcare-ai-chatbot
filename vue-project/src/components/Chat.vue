<template>
  <div class="chat">
    <div class="messages" role="log" aria-live="polite">
      <div v-for="(m, i) in messages" :key="i" :class="['message', m.from]">{{ m.text }}</div>
    </div>

    <div class="input-row">
      <input v-model="input" @keyup.enter="send" placeholder="Type a message..." />
      <button @click="send">Send</button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'

type Msg = { from: 'user' | 'bot'; text: string }

const messages = ref<Msg[]>([])
const input = ref('')

const send = async () => {
  const text = input.value.trim()
  if (!text) return
  messages.value.push({ from: 'user', text })
  input.value = ''
  try {
    const res = await fetch('http://localhost:5184/chat', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ Question: text })
    })
    const data = await res.json()
    const answer = data.Answer ?? data.answer ?? 'No response'
    messages.value.push({ from: 'bot', text: answer })
  } catch (e) {
    messages.value.push({ from: 'bot', text: 'Error contacting API' })
  }
}
</script>

<style scoped>
.chat {
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  padding: 12px;
}
.messages {
  min-height: 200px;
  max-height: 60vh;
  overflow-y: auto;
  padding: 8px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.message {
  padding: 8px 12px;
  border-radius: 12px;
  max-width: 80%;
}
.message.user {
  align-self: flex-end;
  background: #0ea5e9;
  color: white;
}
.message.bot {
  align-self: flex-start;
  background: #f3f4f6;
  color: #111827;
}
.input-row {
  display: flex;
  gap: 8px;
  margin-top: 12px;
}
input {
  flex: 1;
  padding: 8px 10px;
  border: 1px solid #d1d5db;
  border-radius: 6px;
}
button {
  padding: 8px 12px;
  border: none;
  background: #111827;
  color: #fff;
  border-radius: 6px;
}
</style>
