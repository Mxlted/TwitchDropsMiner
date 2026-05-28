import json

from src.utils.json_utils import json_load


def test_json_load_returns_independent_default_values(tmp_path):
    defaults: dict[str, dict[str, list[str]]] = {"nested": {"items": []}}

    loaded = json_load(tmp_path / "missing.json", defaults)
    loaded["nested"]["items"].append("changed")

    assert defaults == {"nested": {"items": []}}


def test_json_load_merges_nested_defaults_without_reusing_template(tmp_path):
    settings_path = tmp_path / "settings.json"
    settings_path.write_text(json.dumps({"nested": {}}), encoding="utf-8")
    defaults: dict[str, dict[str, list[str]]] = {"nested": {"items": []}}

    loaded = json_load(settings_path, defaults)
    loaded["nested"]["items"].append("changed")

    assert defaults == {"nested": {"items": []}}


def test_json_load_preserves_legacy_url_when_default_is_string(tmp_path):
    settings_path = tmp_path / "settings.json"
    settings_path.write_text(
        json.dumps({"proxy": {"__type": "URL", "data": "http://example.test/proxy"}}),
        encoding="utf-8",
    )

    loaded = json_load(settings_path, {"proxy": ""})

    assert loaded["proxy"] == "http://example.test/proxy"
