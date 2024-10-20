import * as React from 'react';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import theme from './theme';
import { useState, useRef, useEffect } from 'react';
import { MinhasConquistas } from './shared/components/minhas-conquistas/MinhasConquistas';
import { Skeleton, TextField } from '@mui/material';
const textToSpeech = (text) => {
  const synth = window.speechSynthesis;
  const utterance = new SpeechSynthesisUtterance(text);
  utterance.pitch = 1;
  utterance.rate = 1;
  synth.speak(utterance);
};

function blobToBuffer(blob) {
  return new Promise((resolve, reject) => {
      const reader = new FileReader();

      reader.onload = function(event) {
          resolve(event.target.result); // Resolvendo com o ArrayBuffer
      };

      reader.onerror = function(err) {
          reject(err); // Rejeitando em caso de erro
      };

      reader.readAsArrayBuffer(blob); // Lê o Blob como ArrayBuffer
  });
}
export default function App() {
  const [media, setMedia] = useState(null);
  const [ative, setAtive] = useState(false);
  const [nome, setNome] = useState('');
  const [renderMinhasConquistas, setRenderMinhasConquistas] = useState(false);
  const [renderSendName, setRenderSendName] = useState(false);
  const videoRef = useRef(null);
  const wsRef = useRef(null);
  const captureIntervalRef = useRef(null);
  const ShouldCaptureIntervalRef = useRef(true);
  const reconnectTimeoutRef = useRef(null); // Armazena o timeout de reconexão

  const constraints = {
    audio: false,
    video: true,
  };

  const handleChangeNome = (e) => {
    if (e && e.target) {
      const txt = e.target.value;
      setNome(txt);
    } else {
      console.error("Evento inválido ou 'target' indefinido.");
    }
  };

  const handleSendNome = () => {
    setRenderSendName(true);
  };

  const handleVideo = async () => {
    if (ative) {
      stopVideoCapture(); // Função para parar a captura de vídeo e fechar WebSocket
    } else {
      startVideoCapture(); // Função para iniciar a captura de vídeo e WebSocket
    }
  };

  const stopVideoCapture = () => {
    if (media) {
      media.getTracks().forEach(track => track.stop());
    }
    clearInterval(captureIntervalRef.current);
    setAtive(false);
    setMedia(null);
    if (wsRef.current) {
      wsRef.current.close();
    }
    console.log("Media stopped and WebSocket closed");
  };

  const startVideoCapture = async () => {
    try {
      const stream = await navigator.mediaDevices.getUserMedia(constraints);
      setMedia(stream);
      setAtive(true);
      console.log("Media started");

      connectWebSocket(stream); // Conecta o WebSocket
    } catch (error) {
      console.error(`getUserMedia error: ${error.name}`, error);
    }
  };

  const connectWebSocket = (stream) => {
    // wsRef.current = new WebSocket('ws://localhost:5017/api/sync/' + nome + (new Date()).toTimeString());
    wsRef.current = new WebSocket('wss://ws.eduardoworrel.com/api/sync/' + nome + (new Date()).toTimeString());

    wsRef.current.onopen = () => {
      console.log('WebSocket connected');
      startImageCapture(stream);
    };

    wsRef.current.onclose = () => {
      console.log('WebSocket closed, attempting to reconnect...');
      reconnectWebSocket(stream); // Chama a função para reconectar
    };

    wsRef.current.onerror = (error) => {
      console.error('WebSocket error:', error);
      wsRef.current.close(); // Fecha o WebSocket ao detectar erro
    };
  };

  const reconnectWebSocket = (stream) => {
    clearTimeout(reconnectTimeoutRef.current); // Limpa qualquer reconexão anterior
    reconnectTimeoutRef.current = setTimeout(() => {
      connectWebSocket(stream); // Tenta reconectar após um atraso
    }, 5000); // Aguarda 5 segundos antes de tentar reconectar
  };

  const startImageCapture = (stream) => {
    wsRef.current.onmessage = (event) => {
      const message = event.data;
      console.log('Received message:', message);
      if (message === "free") {
        ShouldCaptureIntervalRef.current = true;
      } else {
        textToSpeech(message);
      }
    };
    const track = stream.getVideoTracks()[0];
    const imageCapture = new ImageCapture(track);

    captureIntervalRef.current = setInterval(async () => {
      if (ShouldCaptureIntervalRef.current === false) {
        return;
      }
      try {
        const frame = await imageCapture.takePhoto({
          imageHeight:300,
          imageWidth:300
        });
        const buffer = await blobToBuffer(frame);

        if (wsRef.current.readyState === WebSocket.OPEN) {
          ShouldCaptureIntervalRef.current = false;
          wsRef.current.send(buffer);
        }
      } catch (error) {
        console.error('Image capture failed:', error);
      }
    }, 5000);
  };

  useEffect(() => {
    if (media && videoRef.current) {
      videoRef.current.srcObject = media;
    }
  }, [media]);

  const handleMinhasConquistas = () => {
    setRenderMinhasConquistas(prev => !prev);
  };

  return (
    <Box display="flex" flexDirection={'column'} justifyContent="center" alignItems="center" height="100vh" width="100vw" bgcolor={theme.palette.background.paper}>
      {!renderSendName && (
        <Box display={'flex'} justifyContent={'space-between'} width={theme.spacing(50)}>
          <Box>
            <TextField
              label={'Nome do motorista: '}
              onChange={handleChangeNome}
            />
          </Box>
          <Button onClick={handleSendNome} style={{ backgroundColor: theme.palette.primary.dark }}>
            Send
          </Button>
        </Box>
      )}

      {renderSendName && (
        <>
          <Skeleton width={theme.spacing(100)} height={2} />

          <Box height={theme.spacing(40)} width={theme.spacing(40)} display={'flex'} alignItems={'center'} justifyContent={'center'} borderColor={theme.palette.primary.dark}>
            {renderMinhasConquistas && (
              <MinhasConquistas />
            )}
          </Box>

          <Box display="flex" borderRadius={5} padding={15} flexDirection="column" gap={10} width={theme.spacing(40)}>
            {!renderMinhasConquistas && (
              <Button onClick={handleVideo} style={{ backgroundColor: ative ? theme.palette.primary.dark : theme.palette.primary.light }}>
                {ative ? 'Cancel' : 'Start'}
              </Button>
            )}
          </Box>
        </>
      )}
    </Box>
  );
}