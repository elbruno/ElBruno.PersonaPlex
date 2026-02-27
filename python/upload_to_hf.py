"""
Upload exported ONNX models to HuggingFace.

Usage:
    python upload_to_hf.py --source onnx_exports/ --repo elbruno/personaplex-7b-v1-ONNX

Prerequisites:
    - pip install huggingface_hub
    - Set HF_TOKEN environment variable
"""

import argparse
import os
from huggingface_hub import HfApi, create_repo

def upload_to_hf(source_dir: str, repo_id: str):
    """Upload all files from source_dir to a HuggingFace repository."""
    token = os.environ.get("HF_TOKEN")
    if not token:
        raise ValueError("HF_TOKEN environment variable is required")

    api = HfApi(token=token)

    # Create repo if it doesn't exist
    try:
        create_repo(repo_id, repo_type="model", exist_ok=True, token=token)
        print(f"Repository {repo_id} ready")
    except Exception as e:
        print(f"Note: {e}")

    # Upload all files
    print(f"Uploading files from {source_dir} to {repo_id}...")
    api.upload_folder(
        folder_path=source_dir,
        repo_id=repo_id,
        repo_type="model",
    )
    print(f"✅ Upload complete: https://huggingface.co/{repo_id}")

def main():
    parser = argparse.ArgumentParser(description="Upload ONNX models to HuggingFace")
    parser.add_argument("--source", required=True, help="Source directory with ONNX files")
    parser.add_argument("--repo", default="elbruno/personaplex-7b-v1-ONNX",
                        help="HuggingFace repository ID")
    args = parser.parse_args()
    upload_to_hf(args.source, args.repo)

if __name__ == "__main__":
    main()
