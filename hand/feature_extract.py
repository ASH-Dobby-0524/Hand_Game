# feature_extract.py
import numpy as np

def extract_features(landmarks):
    """
    landmarks: numpy array, shape (21, 3)
    """
    lm = landmarks.copy().astype(np.float32)

    #손목 기준 상대좌표
    wrist = lm[0].copy()
    lm -= wrist

    # 손 크기 정규화
    hand_size = np.linalg.norm(lm[9]) + 1e-6
    lm /= hand_size

    # 1차원 벡터로 펼치기
    feat = lm.reshape(-1)
    return feat
