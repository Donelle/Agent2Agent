# Loading Llama 2 into Ollama

Follow these steps to pull down and load the `llama2:latest` model into your Ollama server:

1. **Ensure Ollama Is Running**

   ```bash
   docker-compose up -d ollama
   ```

2. **Access the Ollama CLI**

   ```bash
   docker exec -it ollama /bin/sh
   ```

3. **Pull the Llama 2 Model**

   ```bash
   ollama pull llama2:latest
   ```

   This will download the model weights into `/models/llama2:latest`.

4. **Verify the Installed Models**

   ```bash
   ollama list
   ```

   You should see an entry such as:

   ```
   NAME          TYPE    VERSION    PATH
   llama2        llm     latest     /models/llama2:latest
   ```

5. **Exit the Container Shell**

   ```bash
   exit
   ```

6. **Test an Embedding Request**

   ```bash
   curl -X POST http://localhost:11434/v1/embeddings \
     -H "Content-Type: application/json" \
     -d '{"model":"llama2:latest","input":"Test embedding"}'
   ```

If you receive a JSON response containing an `embedding` array, your setup is working correctly.
